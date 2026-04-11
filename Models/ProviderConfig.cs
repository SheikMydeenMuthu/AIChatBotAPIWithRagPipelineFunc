namespace ChatBotAPIWithRAGPipeline.Models;

/// <summary>
/// Provider-agnostic configuration for media handlers
/// </summary>
public class ProviderConfig
{
    /// <summary>
    /// Name of the provider (e.g., "nvidia", "openai", "anthropic")
    /// </summary>
    public string ProviderName { get; set; } = "nvidia";

    /// <summary>
    /// API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for chat completions endpoint
    /// </summary>
    public string ChatCompletionUrl { get; set; } = "https://integrate.api.nvidia.com/v1/chat/completions";

    /// <summary>
    /// Base URL for image generation endpoint
    /// </summary>
    public string ImageGenerationUrl { get; set; } = "https://ai.api.nvidia.com/v1/genai";

    /// <summary>
    /// Base URL for video generation endpoint
    /// </summary>
    public string VideoGenerationUrl { get; set; } = "https://ai.api.nvidia.com/v1/genai";

    /// <summary>
    /// HTTP client name to use for requests
    /// </summary>
    public string HttpClientName { get; set; } = "default";
}
