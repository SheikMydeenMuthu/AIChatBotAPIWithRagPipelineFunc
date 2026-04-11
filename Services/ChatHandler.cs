using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using ChatBotAPIWithRAGPipeline.Models;
using ChatBotAPIWithRAGPipeline.Services;

namespace ChatBotAPIWithRAGPipeline.Handlers;

/// <summary>
/// Handles chat completion requests for any provider
/// </summary>
public class ChatHandler
{
    public async Task<ChatResponseModel> HandleAsync(string model, string userInput, ProviderConfig providerConfig)
    {
        if (string.IsNullOrEmpty(providerConfig.ApiKey))
            return new ChatResponseModel { AIResponse = "API key not configured." };

        var payload = new
        {
            model = model,
            messages = new[] { new { role = "user", content = userInput } },
            temperature = 0.7,
            max_tokens = 200
        };

        using (var client = new HttpClient())
        {
            var request = new HttpRequestMessage(HttpMethod.Post, providerConfig.ChatCompletionUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", providerConfig.ApiKey);
            request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return new ChatResponseModel { AIResponse = $"{response.StatusCode}: {error}" };
                }

                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ChatCompletion>(data);
                string? content = result?.Choices?.FirstOrDefault()?.Message?.Content;

                return new ChatResponseModel { AIResponse = content ?? "No response" };
            }
            catch (Exception ex)
            {
                return new ChatResponseModel { AIResponse = $"Error: {ex.Message}" };
            }
        }
    }
}
