using Microsoft.AspNetCore.Mvc;
using LLMWebAPI.Models;
using LLMWebAPI.Services;

namespace LLMWebAPI.Controllers;

/// <summary>
/// Ollamaとのチャット機能を提供するコントローラー
/// </summary>
[ApiController]
[Route("llm/[controller]")]
public class OllamaController : ControllerBase
{
    private readonly ILogger<OllamaController> _logger;
    private readonly IOllamaService _ollamaService;
    private const string DefaultModel = "gemma2:2b";

    /// <summary>
    /// OllamaControllerのコンストラクタ
    /// </summary>
    /// <param name="logger">ロガーインスタンス</param>
    /// <param name="ollamaService">Ollamaサービス</param>
    public OllamaController(ILogger<OllamaController> logger, IOllamaService ollamaService)
    {
        _logger = logger;
        _ollamaService = ollamaService;
    }

    /// <summary>
    /// 新規チャットを生成する
    /// </summary>
    /// <param name="request">生成リクエスト</param>
    /// <returns>チャットレスポンス</returns>
    [HttpPost("generate")]
    public async Task<ActionResult<ChatResponse>> Generate([FromBody] GenerateRequest request)
    {
        try
        {
            var id = Guid.NewGuid().ToString("N");
            var model = string.IsNullOrEmpty(request.Model) ? DefaultModel : request.Model;
            
            _logger.LogInformation("Generating new chat. ID: {Id}, Model: {Model}", id, model);
            
            var response = await _ollamaService.ProcessChatAsync(id, model, request.Prompt);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate chat");
            return StatusCode(500, new { message = "Failed to generate chat" });
        }
    }

    /// <summary>
    /// 既存のチャットを継続し、新しいメッセージを追加する
    /// セッションが存在しない場合は新規作成する
    /// </summary>
    /// <param name="request">チャットリクエスト</param>
    /// <returns>チャットレスポンス</returns>
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> ContinueChat([FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation("Continue chat request received. ID: {Id}", request.Id);

            var session = _ollamaService.GetChatSession(request.Id);
            if (session == null)
            {
                return await CreateNewChat(request);
            }

            if (string.IsNullOrEmpty(session.Model))
            {
                return BadRequest(new { message = "Model not set" });
            }

            var response = await _ollamaService.ProcessChatAsync(request.Id, session.Model, request.Prompt);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to continue chat {Id}", request.Id);
            return StatusCode(500, new { message = "Failed to continue chat" });
        }
    }

    /// <summary>
    /// チャットの状態を取得する
    /// </summary>
    /// <param name="id">チャットID</param>
    /// <returns>チャット履歴</returns>
    [HttpGet("chat/{id}")]
    public ActionResult<List<ChatMessage>> GetChat(string id)
    {
        try
        {
            _logger.LogInformation("Get chat request received. ID: {Id}", id);

            var history = _ollamaService.GetChatHistory(id);
            if (!history.Any())
            {
                return NotFound(new { message = "Chat not found" });
            }

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chat {Id}", id);
            return StatusCode(500, new { message = "Failed to get chat history" });
        }
    }

    /// <summary>
    /// 新規チャットを作成する
    /// </summary>
    /// <param name="request">チャットリクエスト</param>
    /// <returns>チャットレスポンス</returns>
    private async Task<ActionResult<ChatResponse>> CreateNewChat(ChatRequest request)
    {
        _logger.LogInformation("Creating new chat session for ID: {Id}", request.Id);
        
        var response = await _ollamaService.ProcessChatAsync(
            request.Id,
            DefaultModel,
            request.Prompt
        );
        
        return Ok(response);
    }
}
