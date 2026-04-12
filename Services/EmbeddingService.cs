using ChatBotAPIWithRAGPipeline.Interfaces;
using ChatBotAPIWithRAGPipeline.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace ChatBotAPIWithRAGPipeline.Services
{
    /// <summary>
    /// Service for generating embeddings using NVIDIA API
    /// Integrates with the RAG pipeline for vector search
    /// </summary>
    public class EmbeddingService : IEmbeddingService
    {
        private readonly RestClient _client;
        private readonly ILLMProvider _llmProvider;
        private readonly ILogger<EmbeddingService> _logger;
        private readonly string _embeddingModel;

        public EmbeddingService(
            ILLMProvider llmProvider,
            ILogger<EmbeddingService> logger)
        {
            _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
            _logger = logger;

            if (string.IsNullOrEmpty(_llmProvider.BaseUrl))
                throw new InvalidOperationException("LLM Provider base URL is not configured");

            try
            {
                _client = new RestClient(_llmProvider.BaseUrl);
                _embeddingModel = _llmProvider.EmbeddingModel;

                _logger.LogInformation($"EmbeddingService initialized with model: {_embeddingModel}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize EmbeddingService");
                throw;
            }
        }

        /// <summary>
        /// Generate embedding for a single text string
        /// </summary>
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    throw new ArgumentException("Text cannot be null or empty", nameof(text));

                _logger.LogInformation($"Generating embedding for text (length: {text.Length})");

                // Prepare request payload for NVIDIA API
                var payload = new
                {
                    model = _embeddingModel,
                    input = new[] { text },
                    encoding_format = "float"
                };

                var request = new RestRequest("/embeddings", Method.Post);
                request.AddHeader("Authorization", $"Bearer {_llmProvider.ApiKey}");
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(payload);

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.LogError($"Embedding API failed: {response.StatusCode} - {response.Content}");
                    throw new InvalidOperationException($"Failed to generate embedding: {response.Content}");
                }

                var embeddingResponse = JsonConvert.DeserializeObject<EmbeddingResponse>(response.Content);

                if (embeddingResponse?.Data == null || embeddingResponse.Data.Count == 0)
                {
                    _logger.LogError("No embedding data returned from API");
                    throw new InvalidOperationException("No embedding data returned from API");
                }

                var embedding = embeddingResponse.Data[0].Embedding;

                _logger.LogInformation($"Successfully generated embedding with dimension: {embedding.Length}");
                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding");
                throw;
            }
        }

        /// <summary>
        /// Generate embeddings for multiple text strings in batch
        /// </summary>
        public async Task<Dictionary<string, float[]>> GenerateEmbeddingsBatchAsync(List<string> texts)
        {
            try
            {
                if (!texts.Any())
                    throw new ArgumentException("Text list cannot be empty", nameof(texts));

                _logger.LogInformation($"Generating embeddings for {texts.Count} texts in batch");

                // Filter out empty strings
                var nonEmptyTexts = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

                if (!nonEmptyTexts.Any())
                    throw new ArgumentException("All texts are empty or whitespace", nameof(texts));

                // Prepare request payload for NVIDIA API
                var payload = new
                {
                    model = _embeddingModel,
                    input = nonEmptyTexts,
                    encoding_format = "float"
                };

                var request = new RestRequest("/embeddings", Method.Post);
                request.AddHeader("Authorization", $"Bearer {_llmProvider.ApiKey}");
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(payload);

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.LogError($"Batch embedding API failed: {response.StatusCode} - {response.Content}");
                    throw new InvalidOperationException($"Failed to generate embeddings: {response.Content}");
                }

                var embeddingResponse = JsonConvert.DeserializeObject<EmbeddingResponse>(response.Content);

                if (embeddingResponse?.Data == null || embeddingResponse.Data.Count == 0)
                {
                    _logger.LogError("No embedding data returned from API");
                    throw new InvalidOperationException("No embedding data returned from API");
                }

                // Create dictionary mapping text to embedding
                var resultDictionary = new Dictionary<string, float[]>();

                for (int i = 0; i < embeddingResponse.Data.Count; i++)
                {
                    var text = nonEmptyTexts[i];
                    var embedding = embeddingResponse.Data[i].Embedding;
                    resultDictionary[text] = embedding;
                }

                _logger.LogInformation($"Successfully generated {resultDictionary.Count} embeddings");
                return resultDictionary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating batch embeddings");
                throw;
            }
        }

        /// <summary>
        /// Response model for NVIDIA embedding API
        /// </summary>
        private class EmbeddingResponse
        {
            [JsonProperty("object")]
            public string Object { get; set; }

            [JsonProperty("data")]
            public List<EmbeddingData> Data { get; set; }

            [JsonProperty("model")]
            public string Model { get; set; }

            [JsonProperty("usage")]
            public TokenUsage Usage { get; set; }
        }

        /// <summary>
        /// Individual embedding data from API response
        /// </summary>
        private class EmbeddingData
        {
            [JsonProperty("object")]
            public string Object { get; set; }

            [JsonProperty("index")]
            public int Index { get; set; }

            [JsonProperty("embedding")]
            public float[] Embedding { get; set; }
        }

        /// <summary>
        /// Token usage statistics from API
        /// </summary>
        private class TokenUsage
        {
            [JsonProperty("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonProperty("total_tokens")]
            public int TotalTokens { get; set; }
        }
    }
}