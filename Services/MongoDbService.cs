using DiffDemo.Models;
using MongoDB.Driver;

namespace DiffDemo.Services;

public class MongoDbService : IMongoDbService
{
    private readonly IMongoClient _mongoClient;
    private readonly MongoDbSettings _settings;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Prompt> _promptsCollection;

    public MongoDbService(IMongoClient mongoClient, MongoDbSettings settings)
    {
        _mongoClient = mongoClient;
        _settings = settings;
        _database = _mongoClient.GetDatabase(_settings.DatabaseName);
        _promptsCollection = _database.GetCollection<Prompt>("Prompts");
    }

    public async Task<Prompt?> GetPromptAsync(string promptId)
    {
        // Get the current (non-archived) version of the prompt
        var filter = Builders<Prompt>.Filter.And(
            Builders<Prompt>.Filter.Eq(p => p.PromptId, promptId),
            Builders<Prompt>.Filter.Eq(p => p.ArchivedDateTime, (DateTime?)null)
        );
        return await _promptsCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Prompt> SavePromptAsync(Prompt prompt)
    {
        // Get all versions to find the maximum version number
        var allVersions = await GetAllPromptVersionsAsync(prompt.PromptId);
        
        int nextVersion = 1;
        Prompt? currentActivePrompt = null;
        bool isExperimental = false;

        if (allVersions.Count > 0)
        {
            // Find the maximum version number
            nextVersion = allVersions.Max(p => p.Version) + 1;
            
            // Find the current active version (if exists)
            currentActivePrompt = allVersions.FirstOrDefault(p => p.ArchivedDateTime == null);
            
            // If there's an active version, archive it
            if (currentActivePrompt != null)
            {
                currentActivePrompt.ArchivedDateTime = DateTime.UtcNow;
                var archiveFilter = Builders<Prompt>.Filter.Eq(p => p.Id, currentActivePrompt.Id);
                await _promptsCollection.ReplaceOneAsync(archiveFilter, currentActivePrompt);
                
                // Preserve IsExperimental from the active version
                isExperimental = currentActivePrompt.IsExperimental;
            }
            else
            {
                // If no active version, use IsExperimental from the most recent version
                var mostRecent = allVersions.OrderByDescending(p => p.Version).FirstOrDefault();
                if (mostRecent != null)
                {
                    isExperimental = mostRecent.IsExperimental;
                }
            }
        }

        // Create new version - clear Id so MongoDB generates a new _id
        prompt.Id = null;
        prompt.Version = nextVersion;
        prompt.UpdatedAt = DateTime.UtcNow;
        prompt.ArchivedDateTime = null; // Ensure new version is not archived
        prompt.IsExperimental = isExperimental;
        await _promptsCollection.InsertOneAsync(prompt);

        return prompt;
    }

    public async Task<List<Prompt>> GetPromptHistoryAsync(string promptId)
    {
        // Get all archived versions of the prompt
        var filter = Builders<Prompt>.Filter.And(
            Builders<Prompt>.Filter.Eq(p => p.PromptId, promptId),
            Builders<Prompt>.Filter.Ne(p => p.ArchivedDateTime, (DateTime?)null)
        );
        var sort = Builders<Prompt>.Sort.Descending(p => p.Version);
        return await _promptsCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<Prompt?> GetPromptVersionAsync(string promptId, int version)
    {
        var filter = Builders<Prompt>.Filter.And(
            Builders<Prompt>.Filter.Eq(p => p.PromptId, promptId),
            Builders<Prompt>.Filter.Eq(p => p.Version, version)
        );
        return await _promptsCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<Prompt>> GetAllPromptVersionsAsync(string promptId)
    {
        // Get all versions (both active and archived) of the prompt
        var filter = Builders<Prompt>.Filter.Eq(p => p.PromptId, promptId);
        var sort = Builders<Prompt>.Sort.Descending(p => p.Version);
        return await _promptsCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<List<Prompt>> GetAllPromptsAsync()
    {
        // Get only active (non-archived) prompts
        var filter = Builders<Prompt>.Filter.Eq(p => p.ArchivedDateTime, (DateTime?)null);
        var sort = Builders<Prompt>.Sort.Descending(p => p.UpdatedAt);
        return await _promptsCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<bool> DeletePromptAsync(string promptId)
    {
        // Delete all versions (both active and archived) of the prompt
        var filter = Builders<Prompt>.Filter.Eq(p => p.PromptId, promptId);
        var deleteResult = await _promptsCollection.DeleteManyAsync(filter);

        return deleteResult.DeletedCount > 0;
    }

    public async Task<bool> SetPromptActiveByVersionAsync(string promptId, int version)
    {
        // First, set all versions of this promptId to inactive
        var allVersionsFilter = Builders<Prompt>.Filter.Eq(p => p.PromptId, promptId);
        var updateAllToFalse = Builders<Prompt>.Update.Set(p => p.IsActivePrompt, false);
        await _promptsCollection.UpdateManyAsync(allVersionsFilter, updateAllToFalse);

        // Then, set the specific version to active
        var versionFilter = Builders<Prompt>.Filter.And(
            Builders<Prompt>.Filter.Eq(p => p.PromptId, promptId),
            Builders<Prompt>.Filter.Eq(p => p.Version, version)
        );
        var updateToActive = Builders<Prompt>.Update.Set(p => p.IsActivePrompt, true);
        var updateResult = await _promptsCollection.UpdateOneAsync(versionFilter, updateToActive);

        return updateResult.ModifiedCount > 0;
    }

    public async Task<bool> SetPromptExperimentalAsync(string promptId, bool isExperimental)
    {
        // Get the current active version
        var existingPrompt = await GetPromptAsync(promptId);
        if (existingPrompt == null)
        {
            return false;
        }

        // Update the IsExperimental flag on the current active version
        var filter = Builders<Prompt>.Filter.Eq(p => p.Id, existingPrompt.Id);
        var update = Builders<Prompt>.Update.Set(p => p.IsExperimental, isExperimental);
        var updateResult = await _promptsCollection.UpdateOneAsync(filter, update);

        return updateResult.ModifiedCount > 0;
    }

    public async Task<bool> SetPromptExperimentalByVersionAsync(string promptId, int version, bool isExperimental)
    {
        // Get the specific version
        var promptVersion = await GetPromptVersionAsync(promptId, version);
        if (promptVersion == null)
        {
            return false;
        }

        // Update the IsExperimental flag on the specific version
        var filter = Builders<Prompt>.Filter.Eq(p => p.Id, promptVersion.Id);
        var update = Builders<Prompt>.Update.Set(p => p.IsExperimental, isExperimental);
        var updateResult = await _promptsCollection.UpdateOneAsync(filter, update);

        return updateResult.ModifiedCount > 0;
    }
}

