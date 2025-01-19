using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using LLMWebAPI.Models;

namespace LLMWebAPI.Services;

/// <summary>
/// OpenAIとの通信を管理し、チャットセッションを処理するサービス
/// </summary>
public class OpenAIService : ILLMService
{
    private readonly ILogger<OpenAIService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;
    private static readonly ConcurrentDictionary<string, ChatSession> ChatSessions = new();

    /// <summary>
    /// OpenAIServiceのコンストラクタ
    /// </summary>
    /// <param name="logger">ロガーインスタンス</param>
    /// <param name="configuration">設定情報</param>
    /// <param name="httpClientFactory"></param>
    public OpenAIService(ILogger<OpenAIService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _defaultModel = configuration["OpenAI:DefaultModel"] ?? "gpt-3.5-turbo";

        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("OpenAI API key is not configured");
        }

        _httpClient = httpClientFactory.CreateClient("OpenAI");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
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

            if (!await IsModelAvailableAsync(model))
            {
                _logger.LogWarning("Model {Model} is not available. Using default model {DefaultModel}", model, _defaultModel);
                model = _defaultModel;
            }

            var session = GetOrCreateSession(id, model);
            await ProcessUserMessageAsync(session, prompt);

            return await CreateSuccessResponseAsync(id, model, session.Messages.Last().Content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenAI API request failed. ID: {Id}, Status: {Status}", id, ex.StatusCode);
            return await CreateErrorResponseAsync(id, model, "OpenAI API request failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat processing failed. ID: {Id}", id);
            return await CreateErrorResponseAsync(id, model, "Internal processing error");
        }
    }

    /// <summary>
    /// 指定されたセッションIDのチャット履歴を取得する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <returns>チャットメッセージのリスト。セッションが存在しない場合は空のリスト</returns>
    public List<ChatMessage> GetChatHistory(string id)
    {
        var session = GetChatSession(id);
        if (session != null) return session.Messages.ToList();
        _logger.LogInformation("Chat history not found for ID: {Id}", id);
        return [];
    }

    /// <summary>
    /// 指定されたセッションIDのチャットセッションを取得する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <returns>チャットセッション。セッションが存在しない場合はnull</returns>
    public ChatSession? GetChatSession(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            _logger.LogWarning("Attempted to get chat session with null or empty ID");
            return null;
        }

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
            var response = await _httpClient.GetAsync("/v1/models");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var models = JsonSerializer.Deserialize<OpenAIModelsResponse>(content);

            return models?.Data.Select(m => m.Id).ToList() ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get models from OpenAI API. Status: {Status}", ex.StatusCode);
            return [];
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
        if (string.IsNullOrEmpty(model))
        {
            return false;
        }

        var models = await GetAvailableModelsAsync();
        return models.Contains(model, StringComparer.OrdinalIgnoreCase);
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
            Messages = [],
        });
    }

    /// <summary>
    /// ユーザーメッセージを処理し、AIからの応答を生成する
    /// </summary>
    /// <param name="session">チャットセッション</param>
    /// <param name="prompt">ユーザーからの入力</param>
    private async Task ProcessUserMessageAsync(ChatSession session, string prompt)
    {
        AddMessage(session, "user", prompt);
        var response = await GenerateResponseAsync(session);
        AddMessage(session, "assistant", response);
    }

    /// <summary>
    /// OpenAIを使用してAI応答を生成する
    /// </summary>
    /// <param name="session">チャットセッション</param>
    /// <returns>生成された応答</returns>
    private async Task<string> GenerateResponseAsync(ChatSession session)
    {
        var options = new ChatCompletionRequest
        {
            Model = session.Model,
            Messages = session.Messages.Select(m => new ChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }).ToList(),
            MaxTokens = 1000,
            Temperature = 0.7f,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            TopP = 0.95f
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", options);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
        return result?.Choices[0].Message.Content ?? throw new Exception("No response from OpenAI");
    }

    /// <summary>
    /// セッションにメッセージを追加する
    /// </summary>
    /// <param name="session">チャットセッション</param>
    /// <param name="role">メッセージの役割（user/assistant）</param>
    /// <param name="content">メッセージ内容</param>
    private void AddMessage(ChatSession session, string role, string content)
    {
        session.Messages.Add(new ChatMessage
        {
            Role = role,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// 成功時のレスポンスを生成する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <param name="model">使用したモデル名</param>
    /// <param name="response">AI応答</param>
    /// <returns>成功レスポンス</returns>
    private static Task<ChatResponse> CreateSuccessResponseAsync(string id, string model, string response)
    {
        return Task.FromResult(new ChatResponse
        {
            Id = id,
            Model = model,
            Response = response,
            Status = ChatStatus.Completed,
        });
    }

    /// <summary>
    /// エラー時のレスポンスを生成する
    /// </summary>
    /// <param name="id">チャットセッションID</param>
    /// <param name="model">使用したモデル名</param>
    /// <param name="errorMessage">エラーメッセージ</param>
    /// <returns>エラーレスポンス</returns>
    private static Task<ChatResponse> CreateErrorResponseAsync(string id, string model, string errorMessage)
    {
        return Task.FromResult(new ChatResponse
        {
            Id = id,
            Model = model,
            Response = errorMessage,
            Status = ChatStatus.Error,
        });
    }
}

