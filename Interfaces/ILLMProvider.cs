namespace ChatBotAPIWithRAGPipeline.Interfaces
{
    public interface ILLMProvider
    {
        string ChatModel { get; }
        string EmbeddingModel { get; }
        string ApiKey { get; }
        string BaseUrl { get; }
    }
}