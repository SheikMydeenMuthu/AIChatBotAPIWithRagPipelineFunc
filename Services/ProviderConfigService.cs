using Microsoft.Extensions.Configuration;
using ChatBotAPIWithRAGPipeline.Models;

namespace ChatBotAPIWithRAGPipeline.Services;

/// <summary>
/// Service to load and manage provider configuration from environment or settings
/// </summary>
public class ProviderConfigService
{
    private readonly IConfiguration _config;

    public ProviderConfigService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Get provider configuration based on configured provider name
    /// </summary>
    public ProviderConfig GetProviderConfig()
    {
        var providerName = _config["PROVIDER_NAME"] ?? "nvidia";
        return GetProviderConfig(providerName);
    }

    /// <summary>
    /// Get provider configuration for a specific provider
    /// </summary>
    public ProviderConfig GetProviderConfig(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "nvidia" => GetNvidiaConfig(),
            "openai" => GetOpenAiConfig(),
            "anthropic" => GetAnthropicConfig(),
            _ => GetNvidiaConfig() // default to Nvidia
        };
    }

    private ProviderConfig GetNvidiaConfig()
    {
        return new ProviderConfig
        {
            ProviderName = "nvidia",
            ApiKey = _config["PROVIDER_API_KEY"] ?? _config["NVIDIA_API_KEY"] ?? string.Empty,
            ChatCompletionUrl = _config["CHAT_COMPLETION_URL"] ?? "https://integrate.api.nvidia.com/v1/chat/completions",
            ImageGenerationUrl = _config["IMAGE_GENERATION_URL"] ?? "https://ai.api.nvidia.com/v1/genai",
            VideoGenerationUrl = _config["VIDEO_GENERATION_URL"] ?? "https://ai.api.nvidia.com/v1/genai",
            HttpClientName = "default"
        };
    }

    private ProviderConfig GetOpenAiConfig()
    {
        return new ProviderConfig
        {
            ProviderName = "openai",
            ApiKey = _config["PROVIDER_API_KEY"] ?? _config["OPENAI_API_KEY"] ?? string.Empty,
            ChatCompletionUrl = _config["CHAT_COMPLETION_URL"] ?? "https://api.openai.com/v1/chat/completions",
            ImageGenerationUrl = _config["IMAGE_GENERATION_URL"] ?? "https://api.openai.com/v1/images/generations",
            VideoGenerationUrl = _config["VIDEO_GENERATION_URL"] ?? "https://api.openai.com/v1/videos/generations",
            HttpClientName = "default"
        };
    }

    private ProviderConfig GetAnthropicConfig()
    {
        return new ProviderConfig
        {
            ProviderName = "anthropic",
            ApiKey = _config["PROVIDER_API_KEY"] ?? _config["ANTHROPIC_API_KEY"] ?? string.Empty,
            ChatCompletionUrl = _config["CHAT_COMPLETION_URL"] ?? "https://api.anthropic.com/v1/messages",
            ImageGenerationUrl = _config["IMAGE_GENERATION_URL"] ?? "",
            VideoGenerationUrl = _config["VIDEO_GENERATION_URL"] ?? "",
            HttpClientName = "default"
        };
    }
}
