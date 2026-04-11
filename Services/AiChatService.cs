using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ChatBotAPIWithRAGPipeline.Handlers;
using ChatBotAPIWithRAGPipeline.Models;

namespace ChatBotAPIWithRAGPipeline.Services
{
    /// <summary>
    /// Service to handle AI chat requests using any provider
    /// Directly uses ChatHandler - can be extended with ImageHandler, VideoHandler later
    /// </summary>
    public class AiChatService : IAiChatService
    {
        private readonly ChatHandler _chatHandler;
        private readonly ProviderConfigService _providerConfigService;

        public AiChatService(ProviderConfigService providerConfigService)
        {
            _chatHandler = new ChatHandler();
            _providerConfigService = providerConfigService;
        }

        public async Task<ChatResponseModel> GetResponseAsync(string userInput, string model, string provider)
        {
            try
            {
                // Get provider configuration (provider-agnostic)
                var providerConfig = _providerConfigService.GetProviderConfig(provider);

                if (string.IsNullOrEmpty(providerConfig.ApiKey))
                    return new ChatResponseModel { AIResponse = "Provider API key not configured." };

                // Handle the request directly with ChatHandler
                return await _chatHandler.HandleAsync(model, userInput, providerConfig);
            }
            catch (Exception ex)
            {
                return new ChatResponseModel { AIResponse = $"Error: {ex.Message}" };
            }
        }
    }
}