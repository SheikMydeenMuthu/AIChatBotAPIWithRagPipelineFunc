using ChatBotAPIWithRAGPipeline.Models;

namespace ChatBotAPIWithRAGPipeline.Interfaces
{
    public interface IDocumentProcessor
    {
        /// <summary>
        /// Chunk generic document content
        /// </summary>
        List<DocumentModel> ChunkDocument(
            string content,
            string documentName,
            int chunkSize = 1000,
            int overlap = 100);

        /// <summary>
        /// Process plain text file
        /// </summary>
        List<DocumentModel> ProcessTextFile(
            string fileName,
            string content,
            int chunkSize = 1000,
            int overlap = 100);

        /// <summary>
        /// Process PDF file
        /// </summary>
        List<DocumentModel> ProcessPdfFile(
            string fileName,
            byte[] pdfContent,
            int chunkSize = 1000,
            int overlap = 100);
    }
}