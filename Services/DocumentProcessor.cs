using ChatBotAPIWithRAGPipeline.Interfaces;

namespace ChatBotAPIWithRAGPipeline.Services
{
    /// <summary>
    /// Service to handle AI chat requests using any provider
    /// Directly uses ChatHandler - can be extended with ImageHandler, VideoHandler later
    /// </summary>
    public class DocumentProcessor : IDocumentProcessor
    {
    }
}