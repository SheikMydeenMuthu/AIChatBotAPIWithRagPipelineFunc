using ChatBotAPIWithRAGPipeline.Interfaces;
using ChatBotAPIWithRAGPipeline.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatBotAPIWithRAGPipeline.Services
{
    public class WebScraper : IWebScraper
    {
        private readonly ILogger<WebScraper> _logger;
        private readonly HttpClient _httpClient;
        private readonly IDocumentProcessor _documentProcessor;

        public WebScraper(ILogger<WebScraper> logger, IHttpClientFactory httpClientFactory, IDocumentProcessor documentProcessor)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("webScraper");
            _documentProcessor = documentProcessor;
        }

        /// <summary>
        /// Scrape content from a URL
        /// </summary>
        public string ScrapeUrl(string url)
        {
            try
            {
                _logger.LogInformation($"Scraping URL: {url}");

                // Validate URL
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    throw new ArgumentException("Invalid URL format", nameof(url));
                }

                // Fetch the webpage
                var response = _httpClient.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var htmlContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                // Parse HTML and extract text content
                var textContent = ExtractTextFromHtml(htmlContent);

                _logger.LogInformation($"Successfully scraped {textContent.Length} characters from {url}");
                return textContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scraping URL: {url}");
                throw;
            }
        }

        /// <summary>
        /// Process scraped web content into document chunks for ingestion
        /// </summary>
        public List<DocumentModel> ProcessWebContent(string url, string content, int chunkSize = 1000, int overlap = 100)
        {
            try
            {
                _logger.LogInformation($"Processing web content from {url}");

                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogWarning($"No content to process for URL: {url}");
                    return new List<DocumentModel>();
                }

                // Clean the content
                var cleanedContent = CleanWebContent(content);

                // Process using the existing document processor
                var documents = _documentProcessor.ChunkDocument(
                    cleanedContent,
                    $"web_{Uri.EscapeDataString(url)}",
                    chunkSize,
                    overlap);

                // Enhance metadata with web-specific information
                foreach (var doc in documents)
                {
                    doc.Title = $"Web content from {url}";
                    doc.Metadata["source_type"] = "web";
                    doc.Metadata["source_url"] = url;
                    doc.Metadata["scraped_at"] = DateTime.UtcNow.ToString("o");
                }

                _logger.LogInformation($"Processed web content into {documents.Count} chunks");
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing web content from URL: {url}");
                throw;
            }
        }

        /// <summary>
        /// Extract text content from HTML
        /// </summary>
        private string ExtractTextFromHtml(string htmlContent)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                // Remove script and style elements
                var nodesToRemove = doc.DocumentNode.SelectNodes("//script | //style | //nav | //footer | //header");
                if (nodesToRemove != null)
                {
                    foreach (var node in nodesToRemove)
                    {
                        node.Remove();
                    }
                }

                // Get text content
                var text = doc.DocumentNode.InnerText;

                // Clean up whitespace
                text = Regex.Replace(text, @"\s+", " ");
                text = text.Trim();

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from HTML");
                // Fallback: return raw content if HTML parsing fails
                return Regex.Replace(htmlContent, @"<[^>]*>", " ").Trim();
            }
        }

        /// <summary>
        /// Clean web content by removing extra whitespace and normalizing
        /// </summary>
        private string CleanWebContent(string text)
        {
            // Remove extra whitespace
            text = Regex.Replace(text, @"\s+", " ");
            // Remove common web artifacts
            text = Regex.Replace(text, @"\b(cookie|privacy policy|terms of service|subscribe|newsletter)\b", "", RegexOptions.IgnoreCase);
            text = text.Trim();
            return text;
        }
    }
}