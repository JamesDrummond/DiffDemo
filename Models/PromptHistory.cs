using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DiffDemo.Models;

public class PromptHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("originalPromptId")]
    public string OriginalPromptId { get; set; } = string.Empty;

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("text")]
    public string Text { get; set; } = string.Empty;

    [BsonElement("archivedAt")]
    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
}

