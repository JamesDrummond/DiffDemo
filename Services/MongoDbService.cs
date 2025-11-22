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
        // Check if prompt already exists (get current active version)
        var existingPrompt = await GetPromptAsync(prompt.PromptId);

        if (existingPrompt != null)
        {
            // Archive the current version by setting ArchivedDateTime
            existingPrompt.ArchivedDateTime = DateTime.UtcNow;
            var archiveFilter = Builders<Prompt>.Filter.Eq(p => p.Id, existingPrompt.Id);
            await _promptsCollection.ReplaceOneAsync(archiveFilter, existingPrompt);

            // Create new version - clear Id so MongoDB generates a new _id
            prompt.Id = null;
            prompt.Version = existingPrompt.Version + 1;
            prompt.UpdatedAt = DateTime.UtcNow;
            prompt.ArchivedDateTime = null; // Ensure new version is not archived
            // Preserve IsExperimental from the existing version
            prompt.IsExperimental = existingPrompt.IsExperimental;
            await _promptsCollection.InsertOneAsync(prompt);
        }
        else
        {
            // New prompt - set initial version and ensure Id is null
            prompt.Id = null;
            prompt.Version = 1;
            prompt.UpdatedAt = DateTime.UtcNow;
            prompt.ArchivedDateTime = null; // Ensure new prompt is not archived
            // Default IsExperimental to false for new prompts
            prompt.IsExperimental = false;
            await _promptsCollection.InsertOneAsync(prompt);
        }

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

