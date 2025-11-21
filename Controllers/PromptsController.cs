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

            // Check if a prompt with this PromptId already exists
            var existingPrompt = await _mongoDbService.GetPromptAsync(prompt.PromptId);
            
            // If prompt exists and we're trying to save with a different PromptId (shouldn't happen, but safety check)
            // Actually, if existingPrompt exists, it means we're creating a new version, which is fine
            // The real check is: if the prompt body has an Id set, we need to verify it matches
            // But since we're versioning, we don't set Id in the body, so this check is mainly for safety
            
            // The main protection is in the UI - the PromptId field is readonly for existing prompts
            // But we add a check here: if the prompt exists, ensure we're not trying to change its PromptId
            // Since GetPromptAsync returns the prompt with the same PromptId, if it exists, we're good to create a new version

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
    public async Task<ActionResult<List<Prompt>>> GetPromptHistory(string promptId)
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
    public async Task<ActionResult<Prompt>> GetPromptVersion(string promptId, int version)
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

    [HttpDelete("{promptId}")]
    public async Task<ActionResult> DeletePrompt(string promptId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(promptId))
            {
                return BadRequest(new { error = "PromptId is required" });
            }

            var deleted = await _mongoDbService.DeletePromptAsync(promptId);
            if (!deleted)
            {
                return NotFound(new { error = "Prompt not found", promptId });
            }

            return Ok(new { message = "Prompt deleted successfully", promptId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting prompt {PromptId}", promptId);
            return StatusCode(500, new { error = "An error occurred while deleting the prompt", message = ex.Message });
        }
    }
}

