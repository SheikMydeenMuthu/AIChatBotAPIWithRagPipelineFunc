using ChatBotAPIWithRAGPipeline.Interfaces;
using ChatBotAPIWithRAGPipeline.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace ChatBotAPIWithRAGPipeline.Services
{
    /// <summary>
    /// Pinecone vector store implementation using REST API
    /// Handles vector indexing, searching, and metadata management
    /// </summary>
    public class PineconeVectorStore : IVectorStore
    {
        private readonly RestClient _client;
        private readonly PineconeConfig _config;
        private readonly ILogger<PineconeVectorStore> _logger;

        public PineconeVectorStore(
            PineconeConfig config,
            ILogger<PineconeVectorStore> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;

            if (string.IsNullOrEmpty(_config.ApiKey))
                throw new InvalidOperationException("Pinecone API Key is not configured");

            if (string.IsNullOrEmpty(_config.IndexHost))
                throw new InvalidOperationException("Pinecone Index Host is not configured");

            try
            {
                // Initialize REST client
                _client = new RestClient(_config.IndexHost);
                _logger.LogInformation($"Pinecone REST client initialized. Index: {_config.IndexName}, Host: {_config.IndexHost}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Pinecone client");
                throw;
            }
        }

        /// <summary>
        /// Search for similar vectors in the index
        /// </summary>
        public async Task<List<RetrievedDocument>> SearchAsync(float[] embedding, int topK)
        {
            try
            {
                if (embedding == null || embedding.Length == 0)
                {
                    _logger.LogWarning("Empty embedding provided for search");
                    return new List<RetrievedDocument>();
                }

                _logger.LogInformation($"Searching Pinecone index '{_config.IndexName}' with topK={topK}, namespace='{_config.Namespace}'");

                var queryPayload = new
                {
                    vector = embedding,
                    topK = topK,
                    @namespace = _config.Namespace,
                    includeMetadata = true
                };

                var request = new RestRequest($"/query", Method.Post);
                request.AddHeader("Api-Key", _config.ApiKey);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(queryPayload);

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.LogError($"Pinecone query failed: {response.StatusCode} - {response.Content}");
                    return new List<RetrievedDocument>();
                }

                var queryResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);
                var documents = new List<RetrievedDocument>();

                if (queryResponse?.matches != null)
                {
                    foreach (var match in queryResponse.matches)
                    {
                        var metadata = match.metadata != null
                            ? JsonConvert.DeserializeObject<Dictionary<string, object>>(match.metadata.ToString())
                            : new Dictionary<string, object>();

                        documents.Add(new RetrievedDocument
                        {
                            Id = match.id,
                            Title = metadata?.ContainsKey("title") == true ? metadata["title"]?.ToString() ?? "Untitled" : "Untitled",
                            Content = metadata?.ContainsKey("content") == true ? metadata["content"]?.ToString() ?? string.Empty : string.Empty,
                            SourceFile = metadata?.ContainsKey("source") == true ? metadata["source"]?.ToString() ?? "unknown" : "unknown",
                            ChunkIndex = metadata?.ContainsKey("chunk_index") == true 
                                ? int.TryParse(metadata["chunk_index"]?.ToString(), out int idx) ? idx : 0 
                                : 0,
                            SimilarityScore = (float?)match.score ?? 0f,
                            Metadata = metadata ?? new Dictionary<string, object>()
                        });
                    }
                }

                _logger.LogInformation($"Retrieved {documents.Count} documents from Pinecone");
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Pinecone index");
                return new List<RetrievedDocument>();
            }
        }

        /// <summary>
        /// Check if the index contains any documents
        /// </summary>
        public async Task<bool> CheckIndexStatusAsync()
        {
            try
            {
                _logger.LogInformation($"Checking Pinecone index '{_config.IndexName}' status");

                // Test with a simple query
                var testVector = new float[_config.Dimension];
                var queryPayload = new
                {
                    vector = testVector,
                    topK = 1,
                    @namespace = _config.Namespace
                };

                var request = new RestRequest("/query", Method.Post);
                request.AddHeader("Api-Key", _config.ApiKey);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(queryPayload);

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.LogWarning($"Index check failed: {response.StatusCode}");
                    return false;
                }

                var queryResponse = JsonConvert.DeserializeObject<dynamic>(response.Content);
                var hasVectors = queryResponse?.matches?.Count > 0;

                _logger.LogInformation($"Index status: {(hasVectors ? "Has vectors" : "Empty")}");
                return hasVectors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Pinecone index status");
                return false;
            }
        }

        /// <summary>
        /// Upsert (insert or update) vectors into the index
        /// </summary>
        public async Task UpsertAsync(string id, float[] embedding, string content, Dictionary<string, object> metadata)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentException("ID cannot be null or empty", nameof(id));

                if (embedding == null || embedding.Length == 0)
                    throw new ArgumentException("Embedding cannot be null or empty", nameof(embedding));

                _logger.LogInformation($"Upserting vector: {id}");

                var finalMetadata = metadata ?? new Dictionary<string, object>();
                if (!finalMetadata.ContainsKey("content"))
                    finalMetadata["content"] = content;

                var upsertPayload = new
                {
                    vectors = new[] {
                        new {
                            id = id,
                            values = embedding,
                            metadata = finalMetadata
                        }
                    },
                    @namespace = _config.Namespace
                };

                var request = new RestRequest("/vectors/upsert", Method.Post);
                request.AddHeader("Api-Key", _config.ApiKey);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(upsertPayload);

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.LogError($"Upsert failed: {response.StatusCode} - {response.Content}");
                    throw new InvalidOperationException($"Failed to upsert vector: {response.Content}");
                }

                _logger.LogInformation($"Successfully upserted vector: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error upserting vector {id}");
                throw;
            }
        }

        /// <summary>
        /// Batch upsert vectors
        /// </summary>
        public async Task UpsertBatchAsync(List<(string Id, float[] Embedding, string Content, Dictionary<string, object> Metadata)> vectors)
        {
            try
            {
                if (!vectors.Any())
                {
                    _logger.LogWarning("Empty vector batch provided for upsert");
                    return;
                }

                _logger.LogInformation($"Batch upserting {vectors.Count} vectors");

                var upsertVectors = vectors.Select(v =>
                {
                    var metadata = v.Metadata ?? new Dictionary<string, object>();
                    if (!metadata.ContainsKey("content"))
                        metadata["content"] = v.Content;

                    return new
                    {
                        id = v.Id,
                        values = v.Embedding,
                        metadata = metadata
                    };
                }).ToArray();

                var upsertPayload = new
                {
                    vectors = upsertVectors,
                    @namespace = _config.Namespace
                };

                var request = new RestRequest("/vectors/upsert", Method.Post);
                request.AddHeader("Api-Key", _config.ApiKey);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(upsertPayload);

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.LogError($"Batch upsert failed: {response.StatusCode} - {response.Content}");
                    throw new InvalidOperationException($"Failed to batch upsert vectors: {response.Content}");
                }

                _logger.LogInformation($"Successfully batch upserted {vectors.Count} vectors");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch upserting vectors");
                throw;
            }
        }

        /// <summary>
        /// Delete a vector from the index
        /// </summary>
        public async Task DeleteAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentException("ID cannot be null or empty", nameof(id));

                _logger.LogInformation($"Deleting vector: {id}");

                var deletePayload = new
                {
                    ids = new[] { id },
                    @namespace = _config.Namespace
                };

                var request = new RestRequest("/vectors/delete", Method.Post);
                request.AddHeader("Api-Key", _config.ApiKey);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(deletePayload);

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.LogError($"Delete failed: {response.StatusCode} - {response.Content}");
                    throw new InvalidOperationException($"Failed to delete vector: {response.Content}");
                }

                _logger.LogInformation($"Successfully deleted vector: {id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting vector {id}");
                throw;
            }
        }

        /// <summary>
        /// Batch delete vectors
        /// </summary>
        public async Task DeleteBatchAsync(List<string> ids)
        {
            try
            {
                if (!ids.Any())
                {
                    _logger.LogWarning("Empty ID list provided for batch delete");
                    return;
                }

                _logger.LogInformation($"Batch deleting {ids.Count} vectors");

                var deletePayload = new
                {
                    ids = ids.ToArray(),
                    @namespace = _config.Namespace
                };

                var request = new RestRequest("/vectors/delete", Method.Post);
                request.AddHeader("Api-Key", _config.ApiKey);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(deletePayload);

                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.LogError($"Batch delete failed: {response.StatusCode} - {response.Content}");
                    throw new InvalidOperationException($"Failed to batch delete vectors: {response.Content}");
                }

                _logger.LogInformation($"Successfully batch deleted {ids.Count} vectors");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch deleting vectors");
                throw;
            }
        }
    }
}