namespace ChatBotAPIWithRAGPipeline.Models
{
    /// <summary>
    /// Configuration for Pinecone vector database
    /// </summary>
    public class PineconeConfig
    {
        /// <summary>
        /// Pinecone API Key for authentication
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Pinecone environment/region (e.g., "us-west4-aws")
        /// </summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Name of the Pinecone index to use for vectors
        /// </summary>
        public string IndexName { get; set; } = "rag-documents";

        /// <summary>
        /// Dimension of vectors (typically 1536 for text-embedding-3-small, 3072 for text-embedding-3-large)
        /// </summary>
        public int Dimension { get; set; } = 3072;

        /// <summary>
        /// Metric type for similarity search (cosine, euclidean, dotproduct)
        /// </summary>
        public string Metric { get; set; } = "cosine";
    }
}