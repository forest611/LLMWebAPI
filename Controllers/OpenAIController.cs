using Microsoft.AspNetCore.Mvc;
using LLMWebAPI.Models;
using LLMWebAPI.Services;

namespace LLMWebAPI.Controllers;

/// <summary>
/// OpenAIとのチャット機能を提供するコントローラー
/// </summary>
[ApiController]
[Route("llm/[controller]")]
public class OpenAIController : ControllerBase
{
    private readonly ILogger<OpenAIController> _logger;
    private readonly OpenAIService _llmService;
    private readonly string _defaultModel;

    /// <summary>
    /// OpenAIControllerのコンストラクタ
    /// </summary>
    /// <param name="logger">ロガーインスタンス</param>
    /// <param name="llmService">LLMサービスインスタンス</param>
    /// <param name="configuration">設定情報</param>
    public OpenAIController(ILogger<OpenAIController> logger, OpenAIService llmService, IConfiguration configuration)
    {
        _logger = logger;
        _llmService = llmService;
        _defaultModel = configuration["OpenAI:DefaultModel"] ?? "gpt-3.5-turbo";
    }

    /// <summary>
    /// 新しいチャットを開始する
    /// </summary>
    /// <param name="request">チャットリクエスト</param>
    /// <returns>チャットレスポンス</returns>
    [HttpPost("generate")]
    public async Task<ActionResult<ChatResponse>> Generate([FromBody] GenerateRequest request)
    {
        try
        {
            var id = Guid.NewGuid().ToString("N");
            var model = string.IsNullOrEmpty(request.Model) ? _defaultModel : request.Model;
            
            _logger.LogInformation("Generating new chat. ID: {Id}, Model: {Model}", id, model);
            
            var response = await _llmService.ProcessChatAsync(id, model, request.Prompt);
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

            var session = _llmService.GetChatSession(request.Id);
            if (session == null)
            {
                return await CreateNewChat(request);
            }

            if (string.IsNullOrEmpty(session.Model))
            {
                return BadRequest(new { message = "Model not set" });
            }

            var response = await _llmService.ProcessChatAsync(request.Id, session.Model, request.Prompt);
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

            var history = _llmService.GetChatHistory(id);
            if (!history.Any())
            {
                return NotFound(new { message = "Chat not found" });
            }

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chat history. ID: {Id}", id);
            return StatusCode(500, new { message = "Failed to get chat history" });
        }
    }

    /// <summary>
    /// 使用可能なモデル一覧を取得
    /// </summary>
    /// <returns>モデル一覧</returns>
    [HttpGet("models")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetModels()
    {
        try
        {
            var models = await _llmService.GetAvailableModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get models");
            return StatusCode(500, new { Error = "Failed to get models" });
        }
    }

    /// <summary>
    /// 新しいチャットを作成する（内部メソッド）
    /// </summary>
    /// <param name="request">チャットリクエスト</param>
    /// <returns>チャットレスポンス</returns>
    private async Task<ActionResult<ChatResponse>> CreateNewChat(ChatRequest request)
    {
        _logger.LogInformation("Creating new chat session for ID: {Id}", request.Id);
        
        var response = await _llmService.ProcessChatAsync(
            request.Id,
            _defaultModel,
            request.Prompt
        );
        
        return Ok(response);
    }
}
