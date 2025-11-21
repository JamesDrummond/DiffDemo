using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DiffDemo.Models;

public class Prompt
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("promptId")]
    public string PromptId { get; set; } = string.Empty;

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("text")]
    public string Text { get; set; } = string.Empty;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("archivedDateTime")]
    public DateTime? ArchivedDateTime { get; set; }

    [BsonElement("isExperimental")]
    public bool IsExperimental { get; set; }

    [BsonElement("isActivePrompt")]
    public bool IsActivePrompt { get; set; }
}

