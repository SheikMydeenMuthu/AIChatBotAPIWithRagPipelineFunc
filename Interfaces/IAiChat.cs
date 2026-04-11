using System.Threading.Tasks;
using ChatBotAPIWithRAGPipeline.Models;

namespace ChatBotAPIWithRAGPipeline.Services
{
    public interface IAiChatService
    {
        Task<ChatResponseModel> GetResponseAsync(string userInput, string model, string provider);
    }
}