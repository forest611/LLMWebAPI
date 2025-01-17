using Microsoft.AspNetCore.Mvc;
using LLMWebAPI.Models;
using LLMWebAPI.Services;

namespace LLMWebAPI.Controllers;

[ApiController]
[Route("llm/[controller]")]
public class OllamaController : ControllerBase
{
    private readonly ILogger<OllamaController> _logger;
    private readonly IOllamaService _ollamaService;
    private static readonly Dictionary<string, ChatResponse> _chatResponses = new();
    private static readonly Dictionary<string, string> _chatModels = new();  // IDに対応するモデルを保持

    public OllamaController(ILogger<OllamaController> logger, IOllamaService ollamaService)
    {
        _logger = logger;
        _ollamaService = ollamaService;
    }

    [HttpPost("generate")]
    public ActionResult<ChatResponse> Generate([FromBody] GenerateRequest request)
    {
        var id = Guid.NewGuid().ToString("N");
        _logger.LogInformation("Generate new chat. ID: {Id}, Model: {Model}", id, request.Model);

        var chatRequest = new ChatRequest
        {
            Id = id,
            Prompt = request.Prompt
        };

        var response = new ChatResponse
        {
            Id = id,
            Model = request.Model,
            Status = ChatStatus.Processing
        };

        _chatResponses[id] = response;
        _chatModels[id] = request.Model;  // モデル情報を保存

        // 非同期で処理を開始
        _ = ProcessChatAsync(chatRequest);

        return Ok(response);
    }

    [HttpPost("chat")]
    public ActionResult<ChatResponse> CreateChat([FromBody] ChatRequest request)
    {
        _logger.LogInformation("Chat request received. ID: {Id}", request.Id);

        if (!_chatModels.TryGetValue(request.Id, out var model))
        {
            return NotFound(new { message = "Chat not found" });
        }

        var response = new ChatResponse
        {
            Id = request.Id,
            Model = model,
            Status = ChatStatus.Processing
        };

        _chatResponses[request.Id] = response;

        // 非同期で処理を開始
        _ = ProcessChatAsync(request);

        return Ok(response);
    }

    [HttpGet("chat/{id}")]
    public ActionResult<ChatResponse> GetChat(string id)
    {
        _logger.LogInformation("Get chat request received. ID: {Id}", id);

        if (!_chatResponses.TryGetValue(id, out var response))
        {
            return NotFound(new { message = "Chat not found" });
        }

        return Ok(response);
    }

    private async Task ProcessChatAsync(ChatRequest request)
    {
        try
        {
            var model = _chatModels[request.Id];
            var response = await _ollamaService.ProcessChatAsync(request.Id, model, request.Prompt);
            _chatResponses[request.Id] = response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request {Id}", request.Id);
            var response = _chatResponses[request.Id];
            response.Status = ChatStatus.Error;
            response.Error = ex.Message;
        }
    }
}
