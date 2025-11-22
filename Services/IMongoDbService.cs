using DiffDemo.Models;

namespace DiffDemo.Services;

public interface IMongoDbService
{
    Task<Prompt?> GetPromptAsync(string promptId);
    Task<Prompt> SavePromptAsync(Prompt prompt);
    Task<List<Prompt>> GetPromptHistoryAsync(string promptId);
    Task<Prompt?> GetPromptVersionAsync(string promptId, int version);
    Task<List<Prompt>> GetAllPromptVersionsAsync(string promptId);
    Task<List<Prompt>> GetAllPromptsAsync();
    Task<bool> DeletePromptAsync(string promptId);
    Task<bool> SetPromptActiveByVersionAsync(string promptId, int version);
    Task<bool> DeactivateAllPromptVersionsAsync(string promptId);
    Task<bool> SetPromptExperimentalAsync(string promptId, bool isExperimental);
    Task<bool> SetPromptExperimentalByVersionAsync(string promptId, int version, bool isExperimental);
}

