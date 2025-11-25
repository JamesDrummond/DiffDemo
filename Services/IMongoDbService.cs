using DiffDemo.Models;

namespace DiffDemo.Services;

public interface IMongoDbService
{
    Task<Prompt?> GetPromptByIdAsync(Guid id);
    Task<Prompt?> GetPromptAsync(Guid promptId);
    Task<Prompt> SavePromptAsync(Prompt prompt);
    Task<List<Prompt>> GetPromptHistoryAsync(Guid promptId);
    Task<Prompt?> GetPromptVersionAsync(Guid promptId, int version);
    Task<List<Prompt>> GetAllPromptVersionsAsync(Guid promptId);
    Task<List<Prompt>> GetAllPromptsAsync();
    Task<bool> DeletePromptAsync(Guid promptId);
    Task<bool> SetPromptActiveByVersionAsync(Guid promptId, int version);
    Task<bool> DeactivateAllPromptVersionsAsync(Guid promptId);
    Task<bool> SetPromptExperimentalAsync(Guid promptId, bool isExperimental);
    Task<bool> SetPromptExperimentalByVersionAsync(Guid promptId, int version, bool isExperimental);
}

