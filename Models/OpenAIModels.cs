using System.Text.Json.Serialization;

namespace LLMWebAPI.Models;

public class OpenAIModelsResponse
{
    [JsonPropertyName("data")] 
    public List<OpenAIModel> Data { get; set; } = [];
}

public class OpenAIModel
{
    [JsonPropertyName("id")] 
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;
    
    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; set; } = string.Empty;
    
    [JsonPropertyName("permission")]
    public List<OpenAIPermission>? Permission { get; set; }
}

public class OpenAIPermission
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    [JsonPropertyName("object")]
    public string? Object { get; set; }
    
    [JsonPropertyName("created")]
    public int Created { get; set; }
    
    [JsonPropertyName("allow_create_engine")]
    public bool AllowCreateEngine { get; set; }
    
    [JsonPropertyName("allow_sampling")]
    public bool AllowSampling { get; set; }
    
    [JsonPropertyName("allow_logprobs")]
    public bool AllowLogprobs { get; set; }
    
    [JsonPropertyName("allow_search_indices")]
    public bool AllowSearchIndices { get; set; }
    
    [JsonPropertyName("allow_view")]
    public bool AllowView { get; set; }
    
    [JsonPropertyName("allow_fine_tuning")]
    public bool AllowFineTuning { get; set; }
    
    [JsonPropertyName("organization")]
    public string? Organization { get; set; }
    
    [JsonPropertyName("group")]
    public string? Group { get; set; }
    
    [JsonPropertyName("is_blocking")]
    public bool IsBlocking { get; set; }
}

public class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }

    [JsonPropertyName("n")]
    public int? N { get; set; }

    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }

    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("presence_penalty")]
    public double? PresencePenalty { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public double? FrequencyPenalty { get; set; }

    [JsonPropertyName("logit_bias")]
    public Dictionary<string, int>? LogitBias { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }
}

public class ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public int Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public Usage? Usage { get; set; }

    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; } = new List<Choice>();
}

public class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class Choice
{
    [JsonPropertyName("message")]
    public ChatMessage Message { get; set; } = new ChatMessage();

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = string.Empty;

    [JsonPropertyName("index")]
    public int Index { get; set; }
}
