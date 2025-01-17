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
            _logger.LogInformation("Processing chat request. ID: {Id}, Model: {Model}", id, model);

            var session = GetOrCreateSession(id, model);
            AddUserMessage(session, prompt);

            var response = await GenerateResponseAsync(session);
            AddAssistantMessage(session, response);

            return CreateSuccessResponse(id, model, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request {Id}", id);
            return CreateErrorResponse(id, model, ex.Message);
        }
    }

    /// <summary>
    /// 指定されたセッションIDのチャット履歴を取得する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <returns>チャットメッセージのリスト。セッションが存在しない場合は空のリスト</returns>
    public List<ChatMessage> GetChatHistory(string id)
    {
        if (_chatSessions.TryGetValue(id, out var session))
        {
            return session.Messages.ToList();
        }
        return new List<ChatMessage>();
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
            Model = model
        });
    }

    /// <summary>
    /// セッションにユーザーメッセージを追加する
    /// </summary>
    /// <param name="session">チャットセッション</param>
    /// <param name="content">メッセージ内容</param>
    private void AddUserMessage(ChatSession session, string content)
    {
        session.Messages.Add(new ChatMessage
        {
            Role = "user",
            Content = content
        });
    }

    /// <summary>
    /// セッションにアシスタントメッセージを追加する
    /// </summary>
    /// <param name="session">チャットセッション</param>
    /// <param name="content">メッセージ内容</param>
    private void AddAssistantMessage(ChatSession session, string content)
    {
        session.Messages.Add(new ChatMessage
        {
            Role = "assistant",
            Content = content
        });
    }

    /// <summary>
    /// Ollamaを使用してAI応答を生成する
    /// </summary>
    /// <param name="session">チャットセッション</param>
    /// <returns>生成された応答</returns>
    private async Task<string> GenerateResponseAsync(ChatSession session)
    {
        var request = new OllamaChatRequest
        {
            Model = session.Model,
            Messages = session.Messages.Select(m => new OllamaChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList()
        };

        var jsonRequest = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{_ollamaBaseUrl}/api/chat", content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(jsonResponse);

            if (ollamaResponse?.Message?.Content == null)
            {
                throw new Exception("Invalid response from Ollama API");
            }

            return ollamaResponse.Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama API");
            throw new Exception("Failed to generate response from Ollama", ex);
        }
    }

    /// <summary>
    /// 成功時のレスポンスを生成する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <param name="model">使用したモデル名</param>
    /// <param name="response">AI応答</param>
    /// <returns>成功レスポンス</returns>
    private ChatResponse CreateSuccessResponse(string id, string model, string response)
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
    /// <param name="errorMessage">エラーメッセージ</param>
    /// <returns>エラーレスポンス</returns>
    private ChatResponse CreateErrorResponse(string id, string model, string errorMessage)
    {
        return new ChatResponse
        {
            Id = id,
            Model = model,
            Status = ChatStatus.Error,
            Error = errorMessage
        };
    }
}
