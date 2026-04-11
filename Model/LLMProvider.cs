using ChatBotAPIWithRAGPipeline.Interfaces;

namespace ChatBotAPIWithRAGPipeline.Models;

public class NvidiaProvider : ILLMProvider
{
    private readonly string _embeddingModel = Environment.GetEnvironmentVariable("NVIDIA_EMBEDDING_MODEL") ?? "nvidia/llama-text-embed-v2";
    private readonly string _chatModel = Environment.GetEnvironmentVariable("NVIDIA_CHAT_MODEL") ?? "meta/llama-3.1-70b-instruct";

    public string ChatModel      => _chatModel;
    public string EmbeddingModel => _embeddingModel;
    public string ApiKey         { get; init; } = string.Empty;
    public string BaseUrl        => "https://integrate.api.nvidia.com/v1/";
}