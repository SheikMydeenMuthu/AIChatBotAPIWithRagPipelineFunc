using System.Threading.Tasks;
using ChatBotAPIWithRAGPipeline.Models;

namespace ChatBotAPIWithRAGPipeline.Interfaces
{
    public interface IAiChatService
    {
        Task<ChatResponseModel> GetResponseAsync(string userInput, string model, string provider);
        Task<ChatResponseModel> GetChatResponseWithRagAsync(ChatRequestModel request);
    }
}