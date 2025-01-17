namespace LLMWebAPI.Models;

public enum ChatStatus
{
    Processing,
    Completed,
    Error
}

public class ChatResponse
{
    public string Id { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public ChatStatus Status { get; set; }
    public string? Error { get; set; }
}
