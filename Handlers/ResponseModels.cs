using Newtonsoft.Json;

namespace ChatBotAPIWithRAGPipeline.Handlers;

/// <summary>
/// Generic response model for chat completions from any provider
/// </summary>
public class ChatCompletion
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("choices")]
    public List<Choice> Choices { get; set; } = new();
}

/// <summary>
/// Generic choice response from chat completions
/// </summary>
public class Choice
{
    [JsonProperty("message")]
    public Message? Message { get; set; }
}

/// <summary>
/// Generic message response from chat completions
/// </summary>
public class Message
{
    [JsonProperty("content")]
    public string? Content { get; set; }
}

// TODO: Uncomment when adding image/video generation support
// public class MediaGeneration
// {
//     [JsonProperty("artifacts")]
//     public List<Artifact>? Artifacts { get; set; }
// }
//
// public class Artifact
// {
//     [JsonProperty("base64")]
//     public string? Base64 { get; set; }
// }

