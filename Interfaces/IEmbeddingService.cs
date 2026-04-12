namespace ChatBotAPIWithRAGPipeline.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<Dictionary<string, float[]>> GenerateEmbeddingsBatchAsync(List<string> texts);
    }
}