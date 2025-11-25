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
    public async Task<ActionResult<Prompt>> GetPrompt(Guid promptId)
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

    [HttpGet("by-id/{id}")]
    public async Task<ActionResult<Prompt>> GetPromptById(Guid id)
    {
        try
        {
            var prompt = await _mongoDbService.GetPromptByIdAsync(id);
            if (prompt == null)
            {
                return NotFound(new { error = "Prompt not found", id });
            }
            return Ok(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prompt by id {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the prompt", message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Prompt>> SavePrompt([FromBody] Prompt prompt)
    {
        try
        {
            if (prompt.PromptId == Guid.Empty)
            {
                prompt.PromptId = Guid.NewGuid();
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

            // Strip IsExperimental from the request - it can only be set via the dedicated endpoint
            prompt.IsExperimental = false;

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
    public async Task<ActionResult<List<Prompt>>> GetPromptHistory(Guid promptId)
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

    [HttpGet("{promptId}/versions")]
    public async Task<ActionResult<List<Prompt>>> GetAllPromptVersions(Guid promptId)
    {
        try
        {
            var versions = await _mongoDbService.GetAllPromptVersionsAsync(promptId);
            return Ok(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all prompt versions for {PromptId}", promptId);
            return StatusCode(500, new { error = "An error occurred while retrieving prompt versions", message = ex.Message });
        }
    }

    [HttpGet("{promptId}/history/{version}")]
    public async Task<ActionResult<Prompt>> GetPromptVersion(Guid promptId, int version)
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
    public async Task<ActionResult> DeletePrompt(Guid promptId)
    {
        try
        {
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

    [HttpPost("{promptId}/versions/{version}/set-active")]
    public async Task<ActionResult> SetPromptActiveByVersion(Guid promptId, int version)
    {
        try
        {
            if (version <= 0)
            {
                return BadRequest(new { error = "Version must be greater than 0" });
            }

            // Verify the version exists
            var promptVersion = await _mongoDbService.GetPromptVersionAsync(promptId, version);
            if (promptVersion == null)
            {
                return NotFound(new { error = "Prompt version not found", promptId, version });
            }

            var success = await _mongoDbService.SetPromptActiveByVersionAsync(promptId, version);
            if (!success)
            {
                return StatusCode(500, new { error = "Failed to set prompt as active" });
            }

            return Ok(new { message = $"Prompt {promptId} version {version} set as active", promptId, version });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting prompt {PromptId} version {Version} as active", promptId, version);
            return StatusCode(500, new { error = "An error occurred while setting the prompt as active", message = ex.Message });
        }
    }

    [HttpPut("{promptId}/experimental")]
    public async Task<ActionResult> SetPromptExperimental(Guid promptId, [FromBody] bool isExperimental)
    {
        try
        {
            // Verify the prompt exists
            var prompt = await _mongoDbService.GetPromptAsync(promptId);
            if (prompt == null)
            {
                return NotFound(new { error = "Prompt not found", promptId });
            }

            var success = await _mongoDbService.SetPromptExperimentalAsync(promptId, isExperimental);
            if (!success)
            {
                return StatusCode(500, new { error = "Failed to set experimental flag" });
            }

            return Ok(new { message = $"Prompt {promptId} experimental flag set to {isExperimental}", promptId, isExperimental });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting experimental flag for prompt {PromptId}", promptId);
            return StatusCode(500, new { error = "An error occurred while setting the experimental flag", message = ex.Message });
        }
    }

    [HttpPut("{promptId}/versions/{version}/experimental")]
    public async Task<ActionResult> SetPromptExperimentalByVersion(Guid promptId, int version, [FromBody] bool isExperimental)
    {
        try
        {
            if (version <= 0)
            {
                return BadRequest(new { error = "Version must be greater than 0" });
            }

            // Verify the version exists
            var promptVersion = await _mongoDbService.GetPromptVersionAsync(promptId, version);
            if (promptVersion == null)
            {
                return NotFound(new { error = "Prompt version not found", promptId, version });
            }

            var success = await _mongoDbService.SetPromptExperimentalByVersionAsync(promptId, version, isExperimental);
            if (!success)
            {
                return StatusCode(500, new { error = "Failed to set experimental flag" });
            }

            return Ok(new { message = $"Prompt {promptId} version {version} experimental flag set to {isExperimental}", promptId, version, isExperimental });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting experimental flag for prompt {PromptId} version {Version}", promptId, version);
            return StatusCode(500, new { error = "An error occurred while setting the experimental flag", message = ex.Message });
        }
    }

    [HttpPut("{promptId}/deactivate-all")]
    public async Task<ActionResult> DeactivateAllPromptVersions(Guid promptId)
    {
        try
        {
            // Verify at least one version exists
            var versions = await _mongoDbService.GetAllPromptVersionsAsync(promptId);
            if (versions == null || versions.Count == 0)
            {
                return NotFound(new { error = "No versions found for prompt", promptId });
            }

            var success = await _mongoDbService.DeactivateAllPromptVersionsAsync(promptId);
            if (!success)
            {
                return StatusCode(500, new { error = "Failed to deactivate all versions" });
            }

            return Ok(new { message = $"All versions of prompt {promptId} have been deactivated", promptId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating all versions for prompt {PromptId}", promptId);
            return StatusCode(500, new { error = "An error occurred while deactivating all versions", message = ex.Message });
        }
    }
}

