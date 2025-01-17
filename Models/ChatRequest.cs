namespace LLMWebAPI.Models;

public class GenerateRequest
{
    public string Model { get; set; } = "llama2";  // デフォルトモデル
    public string Prompt { get; set; } = string.Empty;
}

public class ChatRequest
{
    public string Id { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
}
