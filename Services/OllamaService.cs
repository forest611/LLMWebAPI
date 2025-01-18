using System.Text;
using System.Text.Json;
using LLMWebAPI.Models;
using System.Collections.Concurrent;

namespace LLMWebAPI.Services;

/// <summary>
/// Ollamaとの通信を管理し、チャットセッションを処理するサービス
/// </summary>
public class OllamaService : IOllamaService
{
    private readonly ILogger<OllamaService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _ollamaBaseUrl;
    private static readonly ConcurrentDictionary<string, ChatSession> _chatSessions = new();

    /// <summary>
    /// OllamaServiceのコンストラクタ
    /// </summary>
    /// <param name="logger">ロガーインスタンス</param>
    /// <param name="configuration">設定情報</param>
    public OllamaService(ILogger<OllamaService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
    }

    /// <summary>
    /// チャットリクエストを処理し、AIからの応答を生成する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <param name="model">使用するモデル名</param>
    /// <param name="prompt">ユーザーからの入力</param>
    /// <returns>AI応答を含むチャットレスポンス</returns>
    public async Task<ChatResponse> ProcessChatAsync(string id, string model, string prompt)
    {
        try
        {
            _logger.LogInformation("Processing chat. ID: {Id}, Model: {Model}", id, model);
            var session = GetOrCreateSession(id, model);
            
            await ProcessUserMessage(session, prompt);
            return await CreateSuccessResponse(id, model, session.Messages.Last().Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat processing failed. ID: {Id}", id);
            return CreateErrorResponse(id, model);
        }
    }

    /// <summary>
    /// 指定されたセッションIDのチャット履歴を取得する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <returns>チャットメッセージのリスト。セッションが存在しない場合は空のリスト</returns>
    public List<ChatMessage> GetChatHistory(string id)
    {
        return GetChatSession(id)?.Messages.ToList() ?? new List<ChatMessage>();
    }

    /// <summary>
    /// 指定されたセッションIDのチャットセッションを取得する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <returns>チャットセッション。セッションが存在しない場合はnull</returns>
    public ChatSession? GetChatSession(string id)
    {
        _chatSessions.TryGetValue(id, out var session);
        return session;
    }

    /// <summary>
    /// 指定されたIDのセッションを取得、または新規作成する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <param name="model">使用するモデル名</param>
    /// <returns>チャットセッション</returns>
    private ChatSession GetOrCreateSession(string id, string model)
    {
        return _chatSessions.GetOrAdd(id, _ => new ChatSession
        {
            Id = id,
            Model = model,
            Messages = new List<ChatMessage>()
        });
    }

    /// <summary>
    /// ユーザーメッセージを処理し、AIからの応答を生成する
    /// </summary>
    /// <param name="session">チャットセッション</param>
    /// <param name="prompt">ユーザーからの入力</param>
    private async Task ProcessUserMessage(ChatSession session, string prompt)
    {
        AddMessage(session, "user", prompt);
        var response = await GenerateResponseAsync(session);
        AddMessage(session, "assistant", response);
    }

    /// <summary>
    /// セッションにメッセージを追加する
    /// </summary>
    /// <param name="session">チャットセッション</param>
    /// <param name="role">メッセージの役割（user/assistant）</param>
    /// <param name="content">メッセージ内容</param>
    private void AddMessage(ChatSession session, string role, string content)
    {
        session.Messages.Add(new ChatMessage { Role = role, Content = content });
    }

    /// <summary>
    /// Ollamaを使用してAI応答を生成する
    /// </summary>
    /// <param name="session">チャットセッション</param>
    /// <returns>生成された応答</returns>
    private async Task<string> GenerateResponseAsync(ChatSession session)
    {
        var request = CreateOllamaRequest(session);
        var content = SerializeRequest(request);

        try
        {
            var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/chat", content);
            response.EnsureSuccessStatusCode();

            return await ParseResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama API error");
            throw new Exception("Failed to get response", ex);
        }
    }

    /// <summary>
    /// チャットセッションからOllamaリクエストを作成する
    /// </summary>
    /// <param name="session">チャットセッション</param>
    /// <returns>Ollamaリクエスト</returns>
    private OllamaChatRequest CreateOllamaRequest(ChatSession session)
    {
        return new OllamaChatRequest
        {
            Model = session.Model,
            Messages = session.Messages.Select(m => new OllamaChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList()
        };
    }

    /// <summary>
    /// リクエストをJSONにシリアライズする
    /// </summary>
    /// <param name="request">Ollamaリクエスト</param>
    /// <returns>シリアライズされたコンテンツ</returns>
    private StringContent SerializeRequest(OllamaChatRequest request)
    {
        var jsonRequest = JsonSerializer.Serialize(request);
        return new StringContent(jsonRequest, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Ollamaからのレスポンスを解析する
    /// </summary>
    /// <param name="response">HTTPレスポンス</param>
    /// <returns>AIからの応答テキスト</returns>
    private async Task<string> ParseResponse(HttpResponseMessage response)
    {
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(jsonResponse);

        if (ollamaResponse?.Message?.Content == null)
        {
            throw new Exception("Empty response from Ollama");
        }

        return ollamaResponse.Message.Content;
    }

    /// <summary>
    /// 成功時のレスポンスを生成する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <param name="model">使用したモデル名</param>
    /// <param name="response">AI応答</param>
    /// <returns>成功レスポンス</returns>
    private async Task<ChatResponse> CreateSuccessResponse(string id, string model, string response)
    {
        return new ChatResponse
        {
            Id = id,
            Model = model,
            Response = response,
            Status = ChatStatus.Completed
        };
    }

    /// <summary>
    /// エラー時のレスポンスを生成する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <param name="model">使用したモデル名</param>
    /// <returns>エラーレスポンス</returns>
    private ChatResponse CreateErrorResponse(string id, string model)
    {
        return new ChatResponse
        {
            Id = id,
            Model = model,
            Status = ChatStatus.Error,
            Error = "Chat processing failed"
        };
    }
}
