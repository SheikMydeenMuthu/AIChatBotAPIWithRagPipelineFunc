using ChatBotAPIWithRAGPipeline.Interfaces;
using ChatBotAPIWithRAGPipeline.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ChatBotAPIWithRAGPipeline.Services
{
    /// <summary>
    /// Service for processing and chunking documents from various sources
    /// Supports PDF and plain text files
    /// </summary>
    public class DocumentProcessor : IDocumentProcessor
    {
        private readonly ILogger<DocumentProcessor> _logger;
        private const int DefaultChunkSize = 1000;
        private const int DefaultChunkOverlap = 100;

        public DocumentProcessor(ILogger<DocumentProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Process text content into chunks with overlap
        /// </summary>
        public List<DocumentModel> ChunkDocument(
            string content,
            string documentName,
            int chunkSize = DefaultChunkSize,
            int overlap = DefaultChunkOverlap)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    throw new ArgumentException("Content cannot be empty", nameof(content));

                _logger.LogInformation($"Chunking document '{documentName}' with size={chunkSize}, overlap={overlap}");

                // Clean content
                content = CleanText(content);

                // Split into sentences first for better boundaries
                var sentences = SplitIntoSentences(content);
                var chunks = new List<string>();
                var currentChunk = string.Empty;

                foreach (var sentence in sentences)
                {
                    if ((currentChunk + " " + sentence).Length <= chunkSize)
                    {
                        currentChunk += (string.IsNullOrEmpty(currentChunk) ? "" : " ") + sentence;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(currentChunk))
                        {
                            chunks.Add(currentChunk);
                        }
                        currentChunk = sentence;
                    }
                }

                if (!string.IsNullOrEmpty(currentChunk))
                {
                    chunks.Add(currentChunk);
                }

                // Create DocumentModels from chunks
                var documents = new List<DocumentModel>();

                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunkContent = chunks[i];

                    // Add overlap with previous chunk
                    if (i > 0 && overlap > 0)
                    {
                        var prevChunk = chunks[i - 1];
                        var overlapText = prevChunk.Substring(Math.Max(0, prevChunk.Length - overlap));
                        chunkContent = overlapText + " " + chunkContent;
                    }

                    documents.Add(new DocumentModel
                    {
                        Id = $"{documentName}_chunk_{i}",
                        Title = documentName,
                        Content = chunkContent,
                        ChunkIndex = i,
                        TotalChunks = chunks.Count,
                        Metadata = new Dictionary<string, object>
                        {
                            { "source", documentName },
                            { "chunk_index", i },
                            { "chunk_size", chunkContent.Length },
                            { "created_at", DateTime.UtcNow }
                        }
                    });
                }

                _logger.LogInformation($"Document '{documentName}' split into {documents.Count} chunks");
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error chunking document '{documentName}'");
                throw;
            }
        }

        /// <summary>
        /// Process plain text file
        /// </summary>
        public List<DocumentModel> ProcessTextFile(
            string fileName,
            string content,
            int chunkSize = DefaultChunkSize,
            int overlap = DefaultChunkOverlap)
        {
            _logger.LogInformation($"Processing text file: {fileName}");
            return ChunkDocument(content, fileName, chunkSize, overlap);
        }

        /// <summary>
        /// Process PDF file (basic - extracts all text)
        /// For production, consider using iTextSharp or PdfSharp
        /// </summary>
        public List<DocumentModel> ProcessPdfFile(
            string fileName,
            byte[] pdfContent,
            int chunkSize = DefaultChunkSize,
            int overlap = DefaultChunkOverlap)
        {
            try
            {
                _logger.LogInformation($"Processing PDF file: {fileName}");

                // For now, return a placeholder
                // In production, use iTextSharp or similar
                var content = ExtractTextFromPdf(pdfContent);

                return ChunkDocument(content, fileName, chunkSize, overlap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing PDF '{fileName}'");
                throw;
            }
        }

        /// <summary>
        /// Extract text from PDF (basic implementation)
        /// Replace with proper PDF library for production
        /// </summary>
        private string ExtractTextFromPdf(byte[] pdfContent)
        {
            // Placeholder - requires iTextSharp or PdfSharp NuGet package
            // For MVP, we'll return the filename indication
            _logger.LogWarning("PDF extraction not fully implemented. Please add iTextSharp or PdfSharp NuGet package.");
            
            return "PDF content extraction requires additional NuGet package. " +
                   "Please install 'iTextSharp' or 'PdfSharp' and update this method.";
        }

        /// <summary>
        /// Clean text by removing extra whitespace and normalizing
        /// </summary>
        private string CleanText(string text)
        {
            // Remove extra whitespace
            text = Regex.Replace(text, @"\s+", " ");
            text = text.Trim();
            return text;
        }

        /// <summary>
        /// Split text into sentences for better chunking boundaries
        /// </summary>
        private List<string> SplitIntoSentences(string text)
        {
            // Simple sentence splitter
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            return sentences;
        }
    }
}