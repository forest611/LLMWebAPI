namespace LLMWebAPI.Models;

public class ChatSession
{
    public string Id { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
}

public class ChatMessage
{
    public string Role { get; set; } = "user";  // user or assistant
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
