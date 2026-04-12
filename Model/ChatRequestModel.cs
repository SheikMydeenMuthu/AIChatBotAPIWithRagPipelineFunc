using Newtonsoft.Json;
namespace ChatBotAPIWithRAGPipeline.Models;

public class ChatRequestModel
{
    public string UserInput { get; set; }          // Query
    public string Model { get; set; }               // NVIDIA model name
    public string Provider { get; set; }            // "nvidia"

    // NEW - RAG Options (optional, defaults to true if docs exist)
    [JsonProperty("topK")]
    public int TopK { get; set; } = 5;             // Retrieve 5 docs

    [JsonProperty("useRag")]
    public bool? UseRag { get; set; } = null;      // null = auto-decide

    [JsonProperty("confidenceThreshold")]
    public float ConfidenceThreshold { get; set; } = 0.7f;
}