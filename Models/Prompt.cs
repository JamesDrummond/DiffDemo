using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DiffDemo.Models;

public class Prompt
{
    [BsonId]
    [BsonRepresentation(BsonType.Binary)]
    public Guid Id { get; set; }

    [BsonElement("promptId")]
    [BsonRepresentation(BsonType.Binary)]
    public Guid PromptId { get; set; }

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("text")]
    public string Text { get; set; } = string.Empty;
    
    [BsonElement("createdAtDateTime")]
    public DateTime CreatedAtDateTime { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAtDateTime")]
    public DateTime? UpdatedAtDateTime { get; set; }

    [BsonElement("archivedAtDateTime")]
    public DateTime? ArchivedAtDateTime { get; set; }

    [BsonElement("isExperimental")]
    public bool IsExperimental { get; set; } = false;

    [BsonElement("isActivePrompt")]
    public bool IsActivePrompt { get; set; } = false;
}

