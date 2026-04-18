using ChatBotAPIWithRAGPipeline.Models;
using System.Collections.Generic;

namespace ChatBotAPIWithRAGPipeline.Interfaces
{
    public interface IWebScraper
    {
        /// <summary>
        /// Scrape content from a URL
        /// </summary>
        string ScrapeUrl(string url);

        /// <summary>
        /// Process scraped web content into document chunks for ingestion
        /// </summary>
        List<DocumentModel> ProcessWebContent(string url, string content, int chunkSize = 1000, int overlap = 100);
    }
}