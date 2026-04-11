using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.SemanticKernel;
using ChatBotAPIWithRAGPipeline.Models;
using ChatBotAPIWithRAGPipeline.Services;
using ChatBotAPIWithRAGPipeline.Interfaces;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddHttpClient("default", client =>
{
    client.Timeout = TimeSpan.FromSeconds(300);
});

builder.Services.AddScoped<ProviderConfigService>();

// Register AI Chat service
builder.Services.AddScoped<IAiChatService, AiChatService>();

// ============== Semantic Kernel Setup (Optional) ==============
var config = builder.Configuration;

var llmProvider = LLMProviderFactory.Create(config);
    
    #pragma warning disable SKEXP0010
    builder.Services
        .AddSingleton<ILLMProvider>(llmProvider)
        .AddKernel()
        .AddOpenAIChatCompletion(
            modelId: llmProvider.ChatModel,
            apiKey: llmProvider.ApiKey,
            endpoint: new Uri(llmProvider.BaseUrl));
    #pragma warning restore SKEXP0010

// ============== Pinecone Setup ==============
// Only register Pinecone if credentials are available
var pineconeApiKey = Environment.GetEnvironmentVariable("PINECONE_API_KEY") ?? config["Pinecone:ApiKey"];
var pineconeEnvironment = Environment.GetEnvironmentVariable("PINECONE_ENVIRONMENT") ?? config["Pinecone:Environment"];
var pineconeIndexName = Environment.GetEnvironmentVariable("PINECONE_INDEX_NAME") ?? config["Pinecone:IndexName"] ?? "rag-documents";

if (!string.IsNullOrEmpty(pineconeApiKey) && !string.IsNullOrEmpty(pineconeEnvironment))
{
    builder.Services.AddSingleton(new PineconeConfig
    {
        ApiKey = pineconeApiKey,
        Environment = pineconeEnvironment,
        IndexName = pineconeIndexName
    });
}

builder.Build().Run();