using Newtonsoft.Json;
namespace ChatBotAPIWithRAGPipeline.Models;

public class RetrievedDocument
{
     /// <summary>
    /// Unique document identifier from Pinecone
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Document title/name
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The actual text content (chunk)
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Similarity score from Pinecone (0-1)
    /// </summary>
    public float SimilarityScore { get; set; }

    /// <summary>
    /// Additional metadata (source file, page number, etc.)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Original document source file
    /// </summary>
    public string SourceFile { get; set; }

    /// <summary>
    /// Chunk index within the document
    /// </summary>
    public int ChunkIndex { get; set; }
}