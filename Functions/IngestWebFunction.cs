using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ChatBotAPIWithRAGPipeline.Models;
using ChatBotAPIWithRAGPipeline.Services;
using ChatBotAPIWithRAGPipeline.Interfaces;
using System.Collections.Generic;

namespace ChatBotAPIWithRAGPipeline.Functions
{
    public class IngestWebFunction
    {
        private readonly ILogger<IngestWebFunction> _logger;
        private readonly IWebScraper _webScraper;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;

        public IngestWebFunction(
            ILogger<IngestWebFunction> logger,
            IWebScraper webScraper,
            IEmbeddingService embeddingService,
            IVectorStore vectorStore)
        {
            _logger = logger;
            _webScraper = webScraper;
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
        }

        [Function("IngestWeb")]
        public async Task<IActionResult> IngestWeb(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ingest/web")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("IngestWeb function triggered.");

                // Parse form data or JSON body
                string url;
                int chunkSize = 1000;
                int chunkOverlap = 100;

                if (req.HasFormContentType)
                {
                    // Handle form data
                    var form = await req.ReadFormAsync();
                    url = form["url"].FirstOrDefault();

                    var chunkSizeStr = form["chunkSize"].FirstOrDefault() ?? "1000";
                    var chunkOverlapStr = form["chunkOverlap"].FirstOrDefault() ?? "100";

                    if (!int.TryParse(chunkSizeStr, out var parsedChunkSize))
                        chunkSize = 1000;
                    else
                        chunkSize = parsedChunkSize;

                    if (!int.TryParse(chunkOverlapStr, out var parsedChunkOverlap))
                        chunkOverlap = 100;
                    else
                        chunkOverlap = parsedChunkOverlap;
                }
                else
                {
                    // Handle JSON body
                    var body = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic data = JsonConvert.DeserializeObject(body);

                    url = data?.url;
                    chunkSize = data?.chunkSize ?? 1000;
                    chunkOverlap = data?.chunkOverlap ?? 100;
                }

                if (string.IsNullOrWhiteSpace(url))
                    return new BadRequestObjectResult("URL is required.");

                // Validate URL format
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    return new BadRequestObjectResult("Invalid URL format.");

                // Scrape the URL
                _logger.LogInformation($"Scraping URL: {url}");
                string content = _webScraper.ScrapeUrl(url);

                if (string.IsNullOrWhiteSpace(content))
                    return new BadRequestObjectResult("No content could be extracted from the URL.");

                // Process web content into chunks
                _logger.LogInformation($"Processing web content into chunks (size: {chunkSize}, overlap: {chunkOverlap})");
                List<DocumentModel> chunks = _webScraper.ProcessWebContent(url, content, chunkSize, chunkOverlap);

                if (!chunks.Any())
                    return new BadRequestObjectResult("No content chunks could be created from the scraped content.");

                _logger.LogInformation($"Created {chunks.Count} chunks from web content");

                // Generate embeddings for all chunks
                _logger.LogInformation("Generating embeddings for chunks...");
                var chunkTexts = chunks.Select(c => c.Content).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsBatchAsync(chunkTexts);

                _logger.LogInformation($"Embedding vector dims: {embeddings.First().Value.Length}");
                // Prepare vectors for Pinecone
                var vectorsToUpsert = new List<(string Id, float[] Embedding, string Content, Dictionary<string, object> Metadata)>();

                foreach (var chunk in chunks)
                {
                    if (embeddings.TryGetValue(chunk.Content, out var embedding))
                    {
                        vectorsToUpsert.Add((
                            chunk.Id,
                            embedding,
                            chunk.Content,
                            chunk.Metadata
                        ));
                    }
                    else
                    {
                        _logger.LogWarning($"No embedding found for chunk {chunk.Id}");
                    }
                }

                // Store vectors in Pinecone
                _logger.LogInformation($"Storing {vectorsToUpsert.Count} vectors in Pinecone...");
                await _vectorStore.UpsertBatchAsync(vectorsToUpsert);

                _logger.LogInformation($"Successfully ingested web content from '{url}'");

                return new OkObjectResult(new
                {
                    message = "Web content ingested successfully",
                    url = url,
                    chunksCreated = chunks.Count,
                    vectorsIndexed = vectorsToUpsert.Count,
                    documentIds = chunks.Select(c => c.Id).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IngestWeb function error.");
                return new ObjectResult(new { error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }
    }
}