using ChatBotAPIWithRAGPipeline.Models;

namespace ChatBotAPIWithRAGPipeline.Interfaces
{
    public interface IRagOrchestrator
    {
        // Check if any documents indexed
        Task<bool> DocumentsExistAsync();

        // Retrieve relevant context
        Task<List<RetrievedDocument>> RetrieveContextAsync(
            string query,
            int topK);

        // Build RAG system prompt
        string BuildSystemPrompt(List<RetrievedDocument> contexts);

        // Calculate confidence based on retrieval scores
        float CalculateConfidence(List<RetrievedDocument> contexts);
    }
}