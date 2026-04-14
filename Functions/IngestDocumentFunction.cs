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
    public class IngestDocumentFunction
    {
        private readonly ILogger<IngestDocumentFunction> _logger;
        private readonly IDocumentProcessor _documentProcessor;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;

        public IngestDocumentFunction(
            ILogger<IngestDocumentFunction> logger,
            IDocumentProcessor documentProcessor,
            IEmbeddingService embeddingService,
            IVectorStore vectorStore)
        {
            _logger = logger;
            _documentProcessor = documentProcessor;
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
        }

        [Function("IngestDocument")]
        public async Task<IActionResult> IngestDocument(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ingest")] HttpRequest req)
        {
            try
            {
                _logger.LogInformation("IngestDocument function triggered.");

                // Parse multipart form data
                if (!req.HasFormContentType)
                    return new BadRequestObjectResult("Request must be form data.");

                var form = await req.ReadFormAsync();
                var file = form.Files.FirstOrDefault();

                if (file == null || file.Length == 0)
                    return new BadRequestObjectResult("No file provided.");
                    
                if (file.Length > 4 * 1024 * 1024)
                    return new BadRequestObjectResult($"File size {file.Length / (1024 * 1024)}MB exceeds 4MB Pinecone limit. Please upload a smaller file.");

                // Get chunk parameters from form
                var chunkSizeStr = form["chunkSize"].FirstOrDefault() ?? "1000";
                var chunkOverlapStr = form["chunkOverlap"].FirstOrDefault() ?? "100";

                if (!int.TryParse(chunkSizeStr, out var chunkSize))
                    chunkSize = 1000;
                if (!int.TryParse(chunkOverlapStr, out var chunkOverlap))
                    chunkOverlap = 100;

                _logger.LogInformation($"Processing file: {file.FileName}, ChunkSize: {chunkSize}, Overlap: {chunkOverlap}");

                // Read file content
                byte[] fileContent;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    fileContent = ms.ToArray();
                }

                // Process document based on file type
                List<DocumentModel> chunks;
                if (file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    chunks = _documentProcessor.ProcessPdfFile(
                        file.FileName,
                        fileContent,
                        chunkSize,
                        chunkOverlap);
                }
                else if (file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    var textContent = System.Text.Encoding.UTF8.GetString(fileContent);
                    chunks = _documentProcessor.ProcessTextFile(
                        file.FileName,
                        textContent,
                        chunkSize,
                        chunkOverlap);
                }
                else
                {
                    return new BadRequestObjectResult("Unsupported file type. Use .txt or .pdf");
                }

                if (!chunks.Any())
                    return new BadRequestObjectResult("No content extracted from file.");

                _logger.LogInformation($"Created {chunks.Count} chunks from document");

                // Generate embeddings for all chunks
                _logger.LogInformation("Generating embeddings for chunks...");
                var chunkTexts = chunks.Select(c => c.Content).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsBatchAsync(chunkTexts);

                _logger.LogInformation($"Ingest vector dims: {embeddings.First().Value.Length}");
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

                _logger.LogInformation($"Successfully ingested document '{file.FileName}'");

                return new OkObjectResult(new
                {
                    message = "Document ingested successfully",
                    fileName = file.FileName,
                    chunksCreated = chunks.Count,
                    vectorsIndexed = vectorsToUpsert.Count,
                    documentIds = chunks.Select(c => c.Id).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IngestDocument function error.");
                return new ObjectResult(new { error = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }
    }
}