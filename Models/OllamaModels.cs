using System.Text.Json.Serialization;

namespace LLMWebAPI.Models;

/// <summary>
/// Ollama APIリクエストモデル
/// </summary>
public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaChatMessage> Messages { get; set; } = new();
}

/// <summary>
/// Ollama APIレスポンスモデル
/// </summary>
public class OllamaChatResponse
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public OllamaChatMessage Message { get; set; } = new();

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

/// <summary>
/// Ollamaチャットメッセージモデル
/// </summary>
public class OllamaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
