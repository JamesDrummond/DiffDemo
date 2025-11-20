using DiffDemo.Models;
using MongoDB.Driver;

namespace DiffDemo.Services;

public class MongoDbService : IMongoDbService
{
    private readonly IMongoClient _mongoClient;
    private readonly MongoDbSettings _settings;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Prompt> _promptsCollection;
    private readonly IMongoCollection<PromptHistory> _historyCollection;

    public MongoDbService(IMongoClient mongoClient, MongoDbSettings settings)
    {
        _mongoClient = mongoClient;
        _settings = settings;
        _database = _mongoClient.GetDatabase(_settings.DatabaseName);
        _promptsCollection = _database.GetCollection<Prompt>("Prompts");
        _historyCollection = _database.GetCollection<PromptHistory>("PromptHistory");
    }

    public async Task<Prompt?> GetPromptAsync(string promptId)
    {
        var filter = Builders<Prompt>.Filter.Eq(p => p.PromptId, promptId);
        return await _promptsCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Prompt> SavePromptAsync(Prompt prompt)
    {
        // Check if prompt already exists
        var existingPrompt = await GetPromptAsync(prompt.PromptId);

        if (existingPrompt != null)
        {
            // Archive the current version to history
            var historyEntry = new PromptHistory
            {
                OriginalPromptId = existingPrompt.PromptId,
                Version = existingPrompt.Version,
                Text = existingPrompt.Text,
                ArchivedAt = DateTime.UtcNow
            };

            await _historyCollection.InsertOneAsync(historyEntry);

            // Increment version and update
            prompt.Version = existingPrompt.Version + 1;
            prompt.UpdatedAt = DateTime.UtcNow;
            
            var filter = Builders<Prompt>.Filter.Eq(p => p.PromptId, prompt.PromptId);
            await _promptsCollection.ReplaceOneAsync(filter, prompt);
        }
        else
        {
            // New prompt - set initial version
            prompt.Version = 1;
            prompt.UpdatedAt = DateTime.UtcNow;
            await _promptsCollection.InsertOneAsync(prompt);
        }

        return prompt;
    }

    public async Task<List<PromptHistory>> GetPromptHistoryAsync(string promptId)
    {
        var filter = Builders<PromptHistory>.Filter.Eq(h => h.OriginalPromptId, promptId);
        var sort = Builders<PromptHistory>.Sort.Descending(h => h.Version);
        return await _historyCollection.Find(filter).Sort(sort).ToListAsync();
    }

    public async Task<PromptHistory?> GetPromptVersionAsync(string promptId, int version)
    {
        var filter = Builders<PromptHistory>.Filter.And(
            Builders<PromptHistory>.Filter.Eq(h => h.OriginalPromptId, promptId),
            Builders<PromptHistory>.Filter.Eq(h => h.Version, version)
        );
        return await _historyCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<Prompt>> GetAllPromptsAsync()
    {
        var sort = Builders<Prompt>.Sort.Descending(p => p.UpdatedAt);
        return await _promptsCollection.Find(_ => true).Sort(sort).ToListAsync();
    }
}

