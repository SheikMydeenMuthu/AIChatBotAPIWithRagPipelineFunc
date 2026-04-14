using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ChatBotAPIWithRAGPipeline.Handlers;
using ChatBotAPIWithRAGPipeline.Models;
using ChatBotAPIWithRAGPipeline.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatBotAPIWithRAGPipeline.Services
{
    /// <summary>
    /// Service to handle AI chat requests with optional RAG augmentation
    /// Routes between RAG and LLM-only modes based on document availability
    /// </summary>
    public class AiChatService : IAiChatService
    {
        private readonly ChatHandler _chatHandler;
        private readonly ProviderConfigService _providerConfigService;
        private readonly IRagOrchestrator _ragOrchestrator;
        private readonly ILogger<AiChatService> _logger;

        public AiChatService(
            ProviderConfigService providerConfigService,
            IRagOrchestrator ragOrchestrator,
            ILogger<AiChatService> logger)
        {
            _chatHandler = new ChatHandler();
            _providerConfigService = providerConfigService;
            _ragOrchestrator = ragOrchestrator;
            _logger = logger;
        }

        /// <summary>
        /// Get response from LLM for direct query (no RAG)
        /// </summary>
        public async Task<ChatResponseModel> GetResponseAsync(string userInput, string model, string provider)
        {
            try
            {
                var providerConfig = _providerConfigService.GetProviderConfig(provider);

                if (string.IsNullOrEmpty(providerConfig.ApiKey))
                    return new ChatResponseModel { AIResponse = "Provider API key not configured." };

                return await _chatHandler.HandleAsync(model, userInput, providerConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetResponseAsync");
                return new ChatResponseModel { AIResponse = $"Error: {ex.Message}" };
            }
        }

        /// <summary>
        /// Get response with intelligent RAG routing
        /// Automatically decides between RAG and LLM-only modes based on document availability
        /// </summary>
        public async Task<ChatResponseModel> GetChatResponseWithRagAsync(ChatRequestModel request)
        {
            try
            {
                _logger.LogInformation("Processing chat request with RAG routing");

                // Decide: use RAG or LLM only?
                var useRag = await ShouldUseRagAsync(request);

                if (useRag)
                {
                    _logger.LogInformation("Using RAG mode for query");
                    return await ProcessRagQueryAsync(request);
                }
                else
                {
                    _logger.LogInformation("Using LLM-only mode for query");
                    return await ProcessLlmOnlyQueryAsync(request);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetChatResponseWithRagAsync");
                return new ChatResponseModel { AIResponse = $"Error: {ex.Message}" };
            }
        }

        /// <summary>
        /// Process query using RAG with document retrieval
        /// </summary>
        private async Task<ChatResponseModel> ProcessRagQueryAsync(ChatRequestModel request)
        {
            try
            {
                // Retrieve relevant documents
                var contexts = await _ragOrchestrator.RetrieveContextAsync(
                    request.UserInput,
                    request.TopK);
                    _logger.LogWarning("Similarity : " +contexts.Where(c => c.SimilarityScore > 0.7).Count() + " documents retrieved with similarity > 0.7");

                contexts = contexts.Where(c => c.SimilarityScore > 0.3).ToList();

                if (!contexts.Any())
                {
                    _logger.LogWarning("No documents retrieved, falling back to LLM-only");
                    return await ProcessLlmOnlyQueryAsync(request);
                }

                // Build augmented prompt with context
                var systemPrompt = _ragOrchestrator.BuildSystemPrompt(contexts);
                
                // Get provider configuration
                var providerConfig = _providerConfigService.GetProviderConfig(request.Provider);

                if (string.IsNullOrEmpty(providerConfig.ApiKey))
                    return new ChatResponseModel { AIResponse = "Provider API key not configured." };

                // Get LLM response with augmented prompt
                var llmResponse = await _chatHandler.HandleAsync(
                    request.Model,
                    systemPrompt + "\n\nUser Question: " + request.UserInput,
                    providerConfig);

                // Calculate confidence score
                var confidenceScore = _ragOrchestrator.CalculateConfidence(contexts);

                // Build response with source documents
                var response = new ChatResponseModel
                {
                    AIResponse = llmResponse.AIResponse,
                    SourceDocuments = contexts.Select(c => new SourceDocument
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Content = c.Content,
                        SimilarityScore = c.SimilarityScore,
                        Metadata = c.Metadata,
                        SourceFile = c.SourceFile
                    }).ToList(),
                    ConfidenceScore = confidenceScore,
                    Mode = "RAG"
                };

                _logger.LogInformation($"RAG query processed successfully with {contexts.Count} documents");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RAG query");
                throw;
            }
        }

        /// <summary>
        /// Process query using LLM only (no document retrieval)
        /// </summary>
        private async Task<ChatResponseModel> ProcessLlmOnlyQueryAsync(ChatRequestModel request)
        {
            try
            {
                var response = await GetResponseAsync(
                    request.UserInput,
                    request.Model,
                    request.Provider);

                response.Mode = "LLM_ONLY";
                response.SourceDocuments = null;

                _logger.LogInformation("LLM-only query processed successfully");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing LLM-only query");
                throw;
            }
        }

        /// <summary>
        /// Determine if RAG should be used for this request
        /// </summary>
        private async Task<bool> ShouldUseRagAsync(ChatRequestModel request)
        {
            // User explicitly wants LLM only
            if (request.UseRag == false)
            {
                _logger.LogInformation("User explicitly disabled RAG");
                return false;
            }


            // Check if documents exist in vector store
            var docsExist = await _ragOrchestrator.DocumentsExistAsync();

            if (request.UseRag == true)
            {
                if (!docsExist)
                {
                    _logger.LogWarning("User requested RAG but no documents available");
                    return false;
                }
                return true;
            }

            // Auto-decide: use RAG if documents exist
            return docsExist;
        }
    }
}