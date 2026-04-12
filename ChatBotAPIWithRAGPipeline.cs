using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ChatBotAPIWithRAGPipeline.Models;
using ChatBotAPIWithRAGPipeline.Services;
using ChatBotAPIWithRAGPipeline.Interfaces;

namespace ChatBotAPIWithRAGPipeline.Functions
{
    public class ChatFunction
    {
        private readonly ILogger<ChatFunction> _logger;
        private readonly IAiChatService _aiChatService;

        public ChatFunction(ILogger<ChatFunction> logger, IAiChatService aiChatService)
        {
            _logger = logger;
            _aiChatService = aiChatService;
        }

        [Function("Ping")]
        public IActionResult Ping([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("Ping function called.");
            return new OkObjectResult(new { message = "Application is running" });
        }

        [Function("Chat")]
        public async Task<IActionResult> Chat([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("Chat function triggered.");

                var body = await new StreamReader(req.Body).ReadToEndAsync();
                var chatRequest = JsonConvert.DeserializeObject<ChatRequestModel>(body);

                if (chatRequest is null)
                    return new BadRequestObjectResult("Invalid request body.");

                if (string.IsNullOrWhiteSpace(chatRequest.UserInput))
                    return new BadRequestObjectResult("UserInput is required.");

                if (string.IsNullOrWhiteSpace(chatRequest.Model))
                    return new BadRequestObjectResult("Model is required.");

                if (string.IsNullOrWhiteSpace(chatRequest.Provider))
                    return new BadRequestObjectResult("Provider is required.");

                var response = await _aiChatService.GetChatResponseWithRagAsync(chatRequest);

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat function error.");
                return new StatusCodeResult(500);
            }
        }
    }
}
