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
        /// Pinecone Index Host/Endpoint URL
        /// </summary>
        public string IndexHost { get; set; } = string.Empty;

        /// <summary>
        /// Name of the Pinecone index to use for vectors
        /// </summary>
        public string IndexName { get; set; } = "rag-documents";

        /// <summary>
        /// Namespace within the index for data isolation
        /// </summary>
        public string Namespace { get; set; } = "default";

        /// <summary>
        /// Dimension of vectors (1024 for NVIDIA embeddings)
        /// </summary>
        public int Dimension { get; set; } = 1024;

        /// <summary>
        /// Metric type for similarity search
        /// </summary>
        public string Metric { get; set; } = "cosine";
    }
}