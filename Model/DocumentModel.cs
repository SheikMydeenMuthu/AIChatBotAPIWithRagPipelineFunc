namespace ChatBotAPIWithRAGPipeline.Models;

/// <summary>
/// Represents a document chunk ready for embedding and storage
/// </summary>
public class DocumentModel
{
    /// <summary>
    /// Unique identifier for this chunk
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Document title/name
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The text content of this chunk
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Index of this chunk within the document
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Total number of chunks for this document
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// Additional metadata (source file, page number, etc.)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// The embedding vector (populated after embedding generation)
    /// </summary>
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Timestamp when chunk was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}