// Services/LLMProviderFactory.cs

using Microsoft.Extensions.Configuration;
using ChatBotAPIWithRAGPipeline.Interfaces;
using ChatBotAPIWithRAGPipeline.Models;

namespace ChatBotAPIWithRAGPipeline.Services
{
    public static class LLMProviderFactory
    {
        public static ILLMProvider Create(IConfiguration config)
        {
            var provider = config["LLM_PROVIDER"] ?? "nvidia";

            return provider.ToLower() switch
            {
                "nvidia" => new NvidiaProvider
                {
                    ApiKey = config["NVIDIA_API_KEY"]
                        ?? throw new InvalidOperationException("NVIDIA_API_KEY not configured")
                },
                // "azureopenai" => new AzureOpenAIProvider
                // {
                //     ApiKey = config["AZURE_OPENAI_KEY"]
                //         ?? throw new InvalidOperationException("AZURE_OPENAI_KEY not configured"),
                //     BaseUrl = config["AZURE_OPENAI_ENDPOINT"]
                //         ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT not configured")
                // },
                // "openai" => new OpenAIProvider
                // {
                //     ApiKey = config["OPENAI_API_KEY"]
                //         ?? throw new InvalidOperationException("OPENAI_API_KEY not configured")
                // },
                _ => throw new InvalidOperationException($"Unknown LLM_PROVIDER: {provider}")
            };
        }
    }
}