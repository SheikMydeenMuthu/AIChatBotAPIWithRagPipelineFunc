
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChatBotAPIWithRAGPipeline.Models;

public class ChatResponseModel
{
    public string AIResponse { get; set; }
    public bool IsImage { get; set; }
    public bool IsVideo { get; set; }
}


public class NVidiaChatCompletion
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("choices")]
    public List<Choice> Choices { get; set; } = new();

    public bool IsImage { get; set; }
    [JsonProperty("artifacts")]
    public List<Artifact>? Artifacts { get; set; }  
}
public class Artifact
{
    [JsonProperty("base64")]
    public string? Base64 { get; set; }

    [JsonProperty("finish_reason")]
    public string? FinishReason { get; set; }
}
public class Choice
{
    [JsonProperty("index")]
    public int Index { get; set; }

    [JsonProperty("message")]
    public Message Message { get; set; } = new();
}

public class Message
{
    [JsonProperty("role")]
    public string Role { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string? Content { get; set; }
}