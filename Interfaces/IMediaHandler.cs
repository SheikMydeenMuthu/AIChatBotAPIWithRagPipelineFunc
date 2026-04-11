using System.Net.Http;
using System.Threading.Tasks;
using ChatBotAPIWithRAGPipeline.Models;

namespace ChatBotAPIWithRAGPipeline.Handlers
{
    /// <summary>
    /// Generic interface for handling different media types (chat, images, videos)
    /// across different AI providers (Nvidia, OpenAI, Anthropic, etc.)
    /// </summary>
    public interface IMediaHandler
    {
        /// <summary>
        /// Determines if this handler can process the given model
        /// </summary>
        bool CanHandle(string model);

        /// <summary>
        /// Handles the request for the specified model using provider configuration
        /// </summary>
        Task<ChatResponseModel> HandleAsync(string model, string userInput, ProviderConfig providerConfig);
    }
}
