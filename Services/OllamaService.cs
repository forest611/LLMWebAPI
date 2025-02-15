using System.Text;
using System.Text.Json;
using LLMWebAPI.Models;
using System.Collections.Concurrent;

namespace LLMWebAPI.Services;

/// <summary>
/// Ollamaとの通信を管理し、チャットセッションを処理するサービス
/// </summary>
public class OllamaService : ILLMService
{
    private readonly ILogger<OllamaService> _logger;
    private readonly HttpClient _httpClient;
    private static readonly ConcurrentDictionary<string, ChatSession> ChatSessions = new();

    /// <summary>
    /// OllamaServiceのコンストラクタ
    /// </summary>
    /// <param name="logger">ロガーインスタンス</param>
    /// <param name="httpClientFactory">HTTPクライアントファクトリ</param>
    public OllamaService(ILogger<OllamaService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Ollama");
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
        return GetChatSession(id)?.Messages.ToList() ?? [];
    }

    /// <summary>
    /// 指定されたセッションIDのチャットセッションを取得する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <returns>チャットセッション。セッションが存在しない場合はnull</returns>
    public ChatSession? GetChatSession(string id)
    {
        ChatSessions.TryGetValue(id, out var session);
        return session;
    }

    /// <summary>
    /// このサービスで利用可能なモデル一覧を取得する
    /// </summary>
    /// <returns>利用可能なモデル名のリスト</returns>
    public async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var models = JsonSerializer.Deserialize<OllamaModelsResponse>(content);

            return models?.Models?.Select(m => m.Name).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available models");
            return [];
        }
    }

    /// <summary>
    /// 指定されたモデルがこのサービスで利用可能かどうかを確認する
    /// </summary>
    /// <param name="model">確認するモデル名</param>
    /// <returns>利用可能な場合はtrue</returns>
    public async Task<bool> IsModelAvailableAsync(string model)
    {
        var models = await GetAvailableModelsAsync();
        return models.Contains(model);
    }

    /// <summary>
    /// 指定されたIDのセッションを取得、または新規作成する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <param name="model">使用するモデル名</param>
    /// <returns>チャットセッション</returns>
    private static ChatSession GetOrCreateSession(string id, string model)
    {
        return ChatSessions.GetOrAdd(id, _ => new ChatSession
        {
            Id = id,
            Model = model,
            Messages = []
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
    private static void AddMessage(ChatSession session, string role, string content)
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
            var response = await _httpClient.PostAsync("/api/chat", content);
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
    private static OllamaChatRequest CreateOllamaRequest(ChatSession session)
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
    private static StringContent SerializeRequest(OllamaChatRequest request)
    {
        var jsonRequest = JsonSerializer.Serialize(request);
        return new StringContent(jsonRequest, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Ollamaからのレスポンスを解析する
    /// </summary>
    /// <param name="response">HTTPレスポンス</param>
    /// <returns>AIからの応答テキスト</returns>
    private static async Task<string> ParseResponse(HttpResponseMessage response)
    {
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(jsonResponse);

        if (ollamaResponse?.Message.Content == null)
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
    private static Task<ChatResponse> CreateSuccessResponse(string id, string model, string response)
    {
        return Task.FromResult(new ChatResponse
        {
            Id = id,
            Model = model,
            Response = response,
            Status = ChatStatus.Completed
        });
    }

    /// <summary>
    /// エラー時のレスポンスを生成する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <param name="model">使用したモデル名</param>
    /// <returns>エラーレスポンス</returns>
    private static ChatResponse CreateErrorResponse(string id, string model)
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
