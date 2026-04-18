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
builder.Services.AddScoped<IDocumentProcessor, DocumentProcessor>();
builder.Services.AddScoped<IRagOrchestrator, RagOrchestrator>();
builder.Services.AddScoped<IVectorStore, PineconeVectorStore>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IWebScraper, WebScraper>();

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
var pineconeApiKey = config["PINECONE_API_KEY"] ?? Environment.GetEnvironmentVariable("PINECONE_API_KEY");
var pineconeIndexHost = config["PINECONE_INDEX_HOST"] ?? Environment.GetEnvironmentVariable("PINECONE_INDEX_HOST");
var pineconeIndexName = config["PINECONE_INDEX_NAME"] ?? Environment.GetEnvironmentVariable("PINECONE_INDEX_NAME") ?? "rag-documents";
var pineconeNamespace = config["PINECONE_NAMESPACE"] ?? Environment.GetEnvironmentVariable("PINECONE_NAMESPACE") ?? "default";

if (!string.IsNullOrEmpty(pineconeApiKey) && !string.IsNullOrEmpty(pineconeIndexHost))
{
    builder.Services.AddSingleton(new PineconeConfig
    {
        ApiKey = pineconeApiKey,
        IndexHost = pineconeIndexHost,
        IndexName = pineconeIndexName,
        Namespace = pineconeNamespace
    });
}

builder.Build().Run();