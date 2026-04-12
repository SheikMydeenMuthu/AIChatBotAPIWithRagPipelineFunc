using ChatBotAPIWithRAGPipeline.Models;

namespace ChatBotAPIWithRAGPipeline.Interfaces
{
    public interface IVectorStore
    {
        Task<List<RetrievedDocument>> SearchAsync(float[] embedding, int topK);
        Task<bool> CheckIndexStatusAsync();
        Task UpsertAsync(string id, float[] embedding, string content, Dictionary<string, object> metadata);
        Task UpsertBatchAsync(List<(string Id, float[] Embedding, string Content, Dictionary<string, object> Metadata)> vectors);
        Task DeleteAsync(string id);
        Task DeleteBatchAsync(List<string> ids);
    }
}