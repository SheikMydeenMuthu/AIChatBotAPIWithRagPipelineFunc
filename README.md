# 🤖 ChatBot RAG Pipeline API

A production-grade **Retrieval-Augmented Generation (RAG)** backend built with **C# on Azure Functions v4**. Supports PDF and Web URL ingestion, vector storage in Pinecone, and multi-provider LLM querying via a clean interface-based architecture.

---

## 🚀 Live Demo

> Deployed on Azure Functions:
> `https://sheikragpipelinefunc.azurewebsites.net/api`

---

## 🏗️ Architecture

```
User Query
    │
    ▼
/api/chat
    │
    ├── UseRag: true  ──► Embed Query (NVIDIA NIM)
    │                         │
    │                         ▼
    │                    Pinecone Vector Search
    │                         │
    │                         ▼
    │                    Inject Context into Prompt
    │
    └── UseRag: false ──► Direct LLM Call
                              │
                              ▼
                         LLM Response (NVIDIA / Azure OpenAI / OpenAI)
```

---

## 📦 Tech Stack

| Layer | Technology |
|---|---|
| Runtime | Azure Functions v4 Isolated Worker |
| Language | C# .NET 10 |
| Embeddings | NVIDIA NIM (`llama-text-embed-v2`) |
| Vector DB | Pinecone |
| LLM Providers | NVIDIA NIM, Azure OpenAI, OpenAI |
| Orchestration | Semantic Kernel *(coming soon)* |
| PDF Parsing | iText7, PdfSharp |
| Web Scraping | HtmlAgilityPack |
| Monitoring | Azure Application Insights |

---

## 📁 Project Structure

```
AIChatBotAPIWithRagPipelineFunc/
├── Functions/          # Azure Function entry points (HTTP triggers)
├── Handlers/           # Request handlers (interface-registry pattern)
├── Interfaces/         # ILLMProvider, IRagOrchestrator, IVectorStore, IEmbeddingService
├── Model/              # Request/response models, PineconeConfig
├── Services/           # AiChatService, EmbeddingService, RagOrchestrator, PineconeVectorStore
├── Files/              # Sample PDF files for testing ingestion
├── Program.cs          # DI registration, Semantic Kernel setup, Pinecone config
├── host.json           # Azure Functions host configuration
└── .gitignore          # local.settings.json excluded (never commit secrets)
```

---

## 🔌 API Endpoints

### Chat

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/chat` | Send a message, optionally with RAG |

**Request body:**
```json
{
  "UserInput": "What is covered in the MAUI enterprise patterns?",
  "Model": "meta/llama3-8b-instruct",
  "Provider": "Nvidia",
  "input_type": "query",
  "UseRag": true
}
```

---

### Ingestion

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/ingest` | Ingest a PDF file into Pinecone |
| POST | `/api/ingest/Web` | Ingest a web URL into Pinecone |

**PDF ingestion** — multipart/form-data:
```
file        = <PDF file>
chunkSize   = 300
chunkOverlap = 50
```

**Web ingestion** — JSON:
```json
{
  "url": "https://example.com/article",
  "chunkSize": "1000",
  "chunkOverlap": "100"
}
```

---

### Pinecone Vector Management (Direct)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/vectors/fetch` | Fetch specific chunks by ID |
| GET | `/vectors/list` | List all vectors for a file prefix |
| POST | `/vectors/delete` | Delete vectors by chunk IDs |

> These endpoints call Pinecone directly. Pass your `Api-Key` header.

---

## ⚙️ Configuration

All secrets are managed via **Azure Function App Settings** (never committed to source).

Create a `local.settings.json` locally (already in `.gitignore`):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "LLM_PROVIDER": "Nvidia",
    "NVIDIA_API_KEY": "your-nvidia-api-key",
    "NVIDIA_BASE_URL": "https://integrate.api.nvidia.com/v1",
    "NVIDIA_CHAT_MODEL": "meta/llama3-8b-instruct",
    "NVIDIA_EMBEDDING_MODEL": "nvidia/llama-text-embed-v2",
    "PINECONE_API_KEY": "your-pinecone-api-key",
    "PINECONE_INDEX_HOST": "your-pinecone-index-host",
    "PINECONE_INDEX_NAME": "rag-documents",
    "PINECONE_NAMESPACE": "default"
  }
}
```

---

## 🏃 Running Locally

**Prerequisites:**
- .NET 10 SDK
- Azure Functions Core Tools v4
- Pinecone account + index
- NVIDIA NIM API key

```bash
# Clone
git clone https://github.com/SheikMydeenMuthu/AIChatBotAPIWithRagPipelineFunc.git
cd AIChatBotAPIWithRagPipelineFunc

# Configure
# Create local.settings.json with your keys (see above)

# Run
func start
```

API will be available at `http://localhost:7071/api/`

---

## 🧪 Testing with Sample Files

Sample PDF files are included in the `/Files` folder for testing ingestion:

```
Files/
└── Enterprise-Application-Patterns-Using-.NET-MAUI.pdf
```

Use the `/api/ingest` endpoint to push this PDF into Pinecone, then query it via `/api/chat` with `UseRag: true`.

---

## 🗺️ Roadmap

- [x] PDF ingestion + chunking
- [x] Web URL ingestion + scraping
- [x] NVIDIA NIM embeddings
- [x] Pinecone vector storage
- [x] Multi-provider LLM abstraction (NVIDIA / Azure OpenAI / OpenAI)
- [x] RAG query pipeline with fallback
- [ ] Semantic Kernel orchestration layer
- [ ] Real-time web fetch for live grounding
- [ ] Agentic RAG with multi-step reasoning

---

## 🔐 Security Notes

- `local.settings.json` is excluded via `.gitignore` — never commit it
- All API keys stored as Azure Function App environment variables
- Azure Function `?code=` access key required for all app endpoints

---

## 👤 Author

**Sheik Mydeen Muthu**
Lead Mobility Engineer · AI-aware Mobile Lead

- LinkedIn: [linkedin.com/in/sheikmydeenmuthu](https://linkedin.com/in/sheikmydeenmuthu)
- GitHub: [github.com/SheikMydeenMuthu](https://github.com/SheikMydeenMuthu)

---

## 📄 License

MIT License — feel free to use, fork, and contribute.
