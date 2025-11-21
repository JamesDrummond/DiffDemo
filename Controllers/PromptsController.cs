using Microsoft.AspNetCore.Mvc;
using DiffDemo.Models;
using DiffDemo.Services;

namespace DiffDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromptsController : ControllerBase
{
    private readonly IMongoDbService _mongoDbService;
    private readonly ILogger<PromptsController> _logger;

    public PromptsController(IMongoDbService mongoDbService, ILogger<PromptsController> logger)
    {
        _mongoDbService = mongoDbService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Prompt>>> GetAllPrompts()
    {
        try
        {
            var prompts = await _mongoDbService.GetAllPromptsAsync();
            return Ok(prompts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all prompts");
            return StatusCode(500, new { error = "An error occurred while retrieving prompts", message = ex.Message });
        }
    }

    [HttpGet("{promptId}")]
    public async Task<ActionResult<Prompt>> GetPrompt(string promptId)
    {
        try
        {
            var prompt = await _mongoDbService.GetPromptAsync(promptId);
            if (prompt == null)
            {
                return NotFound(new { error = "Prompt not found", promptId });
            }
            return Ok(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prompt {PromptId}", promptId);
            return StatusCode(500, new { error = "An error occurred while retrieving the prompt", message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Prompt>> SavePrompt([FromBody] Prompt prompt)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prompt.PromptId))
            {
                return BadRequest(new { error = "PromptId is required" });
            }

            if (string.IsNullOrWhiteSpace(prompt.Text))
            {
                return BadRequest(new { error = "Prompt text is required" });
            }

            var savedPrompt = await _mongoDbService.SavePromptAsync(prompt);
            return Ok(savedPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving prompt {PromptId}", prompt.PromptId);
            return StatusCode(500, new { error = "An error occurred while saving the prompt", message = ex.Message });
        }
    }

    [HttpGet("{promptId}/history")]
    public async Task<ActionResult<List<PromptHistory>>> GetPromptHistory(string promptId)
    {
        try
        {
            var history = await _mongoDbService.GetPromptHistoryAsync(promptId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prompt history for {PromptId}", promptId);
            return StatusCode(500, new { error = "An error occurred while retrieving prompt history", message = ex.Message });
        }
    }

    [HttpGet("{promptId}/history/{version}")]
    public async Task<ActionResult<PromptHistory>> GetPromptVersion(string promptId, int version)
    {
        try
        {
            var historyItem = await _mongoDbService.GetPromptVersionAsync(promptId, version);
            if (historyItem == null)
            {
                return NotFound(new { error = "Prompt version not found", promptId, version });
            }
            return Ok(historyItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prompt version {PromptId} v{Version}", promptId, version);
            return StatusCode(500, new { error = "An error occurred while retrieving the prompt version", message = ex.Message });
        }
    }
}

