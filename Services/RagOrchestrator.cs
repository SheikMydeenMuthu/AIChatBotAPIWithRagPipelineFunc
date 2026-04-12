using ChatBotAPIWithRAGPipeline.Interfaces;
using ChatBotAPIWithRAGPipeline.Models;
using Microsoft.Extensions.Logging;

namespace ChatBotAPIWithRAGPipeline.Services
{
    /// <summary>
    /// Orchestrates RAG operations: retrieval, augmentation, and confidence scoring
    /// </summary>
    public class RagOrchestrator : IRagOrchestrator
    {
        private readonly IVectorStore _vectorStore;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<RagOrchestrator> _logger;

        public RagOrchestrator(
            IVectorStore vectorStore,
            IEmbeddingService embeddingService,
            ILogger<RagOrchestrator> logger)
        {
            _vectorStore = vectorStore;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        /// <summary>
        /// Check if any documents are indexed in Pinecone
        /// </summary>
        public async Task<bool> DocumentsExistAsync()
        {
            try
            {
                var result = await _vectorStore.CheckIndexStatusAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if documents exist");
                return false;
            }
        }

        /// <summary>
        /// Retrieve relevant documents from Pinecone based on query similarity
        /// </summary>
        public async Task<List<RetrievedDocument>> RetrieveContextAsync(string query, int topK)
        {
            try
            {
                // Generate embedding for the query
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

                // Search Pinecone for similar vectors
                var results = await _vectorStore.SearchAsync(queryEmbedding, topK);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving context from vector store");
                return new List<RetrievedDocument>();
            }
        }

        /// <summary>
        /// Build system prompt with retrieved documents as context
        /// </summary>
        public string BuildSystemPrompt(List<RetrievedDocument> contexts)
        {
            if (!contexts.Any())
            {
                return "You are a helpful assistant. Answer based on your knowledge.";
            }

            // Build context string from retrieved documents
            var contextBuilder = new System.Text.StringBuilder();
            contextBuilder.AppendLine("You are a helpful assistant. Use the following context to answer the user's question:");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("--- CONTEXT START ---");

            foreach (var doc in contexts)
            {
                contextBuilder.AppendLine($"Source: {doc.SourceFile} (Relevance: {doc.SimilarityScore:P2})");
                contextBuilder.AppendLine($"Content: {doc.Content}");
                contextBuilder.AppendLine();
            }

            contextBuilder.AppendLine("--- CONTEXT END ---");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("If the context doesn't contain relevant information, say so and provide your best answer.");

            return contextBuilder.ToString();
        }

        /// <summary>
        /// Calculate overall confidence score based on retrieved documents' similarity scores
        /// </summary>
        public float CalculateConfidence(List<RetrievedDocument> contexts)
        {
            if (!contexts.Any())
                return 0f;

            // Average similarity score of top results
            var averageScore = (float)contexts.Average(c => c.SimilarityScore);

            // Ensure score is between 0 and 1
            return Math.Clamp(averageScore, 0f, 1f);
        }
    }
}