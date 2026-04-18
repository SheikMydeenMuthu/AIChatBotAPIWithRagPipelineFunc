using System.ComponentModel.DataAnnotations;

namespace ChatBotAPIWithRAGPipeline.Models
{
    public class WebChatRequestModel
    {
        [Required]
        public string Url { get; set; }

        [Required]
        public string UserInput { get; set; }

        [Required]
        public string Model { get; set; }

        [Required]
        public string Provider { get; set; }

        public int? ChunkSize { get; set; }

        public int? ChunkOverlap { get; set; }
    }
}