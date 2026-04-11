namespace ChatBotAPIWithRAGPipeline.Models;

    public class ChatRequestModel
    {
        public string UserInput { get; set; }
        public string Model { get; set; }
        public string Provider { get; set; }
    }