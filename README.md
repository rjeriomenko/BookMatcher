# BookMatcher

A fuzzy book search system that uses one of three large language models (Gemini Flash Lite 2.5, Gemini Flash 2.5, OpenAI 4o Mini) to interpret messy, incomplete queries and match them to real books from the OpenLibrary API.
The application handles queries with natural language descriptions, partial titles, misspellings, author names, and keywords.
The user can speecify an LLM model and the temperature the LLM should use.

**Live Demo:** https://book-matcher-rosy.vercel.app

**API Endpoint:** https://bookmatcher-production.up.railway.app

## Quick Start

### Prerequisites

- **Option 1 (Docker):** Docker Desktop installed
- **Option 2 (.NET):** .NET 10 SDK installed

### Setup and Run

Run the setup script:

```bash
./start.sh
```

The script will:
- Prompt you for API keys (Gemini and OpenAI)
- Let you choose between Docker or .NET CLI
- Start the server at `http://localhost:5000`

### Swagger

- If you run the backend locally, you can view and make requests to the BookMatch endpoint at:
- http://localhost:5282/swagger/index.html

### Configuration Management

Single `appsettings.json` file:
- Local development: Real keys in file (user provides them in appsettings.json)
- Docker: Mounted as volume
- Production: Environment variables override file values

### Demo Live Deployment
Backend:
Railway auto-deploys from GitHub `main` branch

Frontend: Vercel auto-deploys from GitHub `main` branch

## Architecture Overview

### System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                          User Query                             │
│                   "there and back again"                        │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │   React Frontend     │
                    │   (Vercel)           │
                    └──────────┬───────────┘
                               │ HTTP
                               ▼
┌────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                   ASP.NET Core API (Railway)                                                       │
│                                                                                                    │
│  ┌────────────────────────────────────────────────────────────┐                                    │
│  │ BookMatchController                                        │                                    │
│  │ • Validates query                                          │                                    │
│  │ • Returns 200/404/503/500                                  │                                    │
│  └────────────────────┬───────────────────────────────────────┘                                    │
│                       │                                                                            │
│                       ▼                                                                            │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐              │
│  │ BookMatchService                                                                 │              │
│  │ • Orchestrates LLM + OpenLibrary workflow                                        │              │
│  └────────┬───────────────────────────────────────────────────────────┬─────────────┘              │
│           │                                                           │                            │
│           ▼                                                           ▼                            │
│  ┌──────────────────────────────────────────────────┐      ┌───────────────────────────────┐       │
│  │  LlmService                                      │      │ OpenLibraryService            │       │
│  │  • Uses SK Kernel                                │      │ • Performs book search        │       │
│  │  • Creates Chat Completion Service               │      │ • Retrieves book metadata     │       │
│  │  • Sends prompt, query, and JSON output schema   │      │ • Retrieves cover images      │       │
│  └────┬─────────────────────────────────────────────┘      └─────────┬─────────────────────┘       │
│       │                                                              │                             │
└───────┼──────────────────────────────────────────────────────────────┼─────────────────────────────┘
        │                                                              │
        ▼                                                              ▼
┌─────────────────────────────────┐                            ┌──────────────────┐
│ • Google Gemini Flash Lite 2.5  │                            │  OpenLibrary API │
│ • Google Gemini Flash 2.5       │                            └──────────────────┘
│ • OpenAI GPT-4o Mini            │                              
└─────────────────────────────────┘                             

```
## How It Works

1. **LLM Processing Step**
   - Frontend submits user's messy query (e.g., "there and back again") as query param to GET api/bookMatch/match
   - BookMatchController uses BookMatchService to orchestrate book matching flow
   - BookMatchService uses LlmService to prompt LLM APIs to generate hypotheses for books that the user's query matches
   - LlmService creates a ChatCompletionService instance with Semantic Kernel
   - ChatCompletionService instance is configured with appsettings config values and the LlmBookHypothesisResponseSchema type
   - Semantic Kernel serializes LlmBookHypothesisResponseSchema into JSON schema, which will enforce constraints on the LLM's response format
   - JSON schema is sent (along with LLM prompt as system message and user's original query as user message) in HTTP request to Google/OpenAI LLM API
   - LLM API generates up to five hypotheses, with reasoning and confidence scoring, from user's query based on matching hierarchy
   - LLM API responds with HTTP message structured to match JSON schema
   - The JSON in the LLM HTTP response is deserialized into an instance of the LlmBookHypothesisResponseSchema

2. **OpenLibrary Search Step**
   - Each LLM hypothesis has a LlmBookHypothesis in the deserialized LlmBookHypothesisResponseSchema
   - BookMatchService uses OpenLibraryService to retrieve candidates for books that match the hypotheses generated by the LLM
   - OpenLibraryService uses the fields in each LlmBookHypothesis to make up to three search queries to OpenLibrary's API
   - OpenLibraryService starts with a precise search for book match candidates using all the fields provided in the LlmBookHypothesis
   - OpenLibraryService will sequentially make broader searches for matching books if the precise search returns nothing
   - Up to five book match candidates are retrieved as OpenLibraryWorkDocuments in an OpenLibraryWorkSearchResponse

3. **LLM Ranking Step**
   - BookMatchService uses LlmService to prompt LLM APIs to choose best book match candidate for each hypothesis, rank and re-order the best matches, and separate the primary author(s) from the contributors
   - Once again, LlmService creates a ChatCompletionService instance with Semantic Kernel
   - Semantic Kernel generates JSON response schema with LlmRankedMatchResponseSchema
   - Semantic Kernel sends JSON schema (along with LLM prompt as system message and hypotheses + book candidates as user message) in HTTP request to Google/OpenAI LLM API
   - LLM API generates ranked results ordered (with OpenLibrary work key) by best match to user's original query using matching hierarchy
   - LLM API responds with HTTP message structured to match JSON schema
   - The JSON in the LLM HTTP response is deserialized into an instance of the LlmRankedMatchResponseSchema

4. **OpenLibrary Metadata Fetching and Response Mapping Step**
   - BookMatchService uses OpenLibraryService to fetch an edition for each matched work
   - BookMatchService uses OpenLibraryService to fetch the cover ID for each matched work
   - The fetched edition is used to generate an OpenLibrary link for each matched work
     - This is important because, without specifying the edition, the link can lead to editions in other languages (despite the work being in English)
   - The fetched cover ID is used to generate an OpenLibrary cover url for each matched work
   - The edition url, the cover url, and all the previously fetched work metadata (title, publish year) are mapped alongside the deserialized LlmRankedMatchResponseSchema to instantiate a BookMatchResponse for the client
   - BookMatchController serializes BookMatchResponse into JSON and sends HTTP response to client

### Matching Hierarchy Used
   - Exact/normalized title + primary author match (strongest)
   - Exact/normalized title + contributor-only author (lower rank)
   - Near-match title + author match (candidate)
   - Author-only fallback → return top works by that author
   - If no clear winner, return up to 5 ordered candidates with explanations


## Technology Stack

### Backend
- **.NET 10** - Modern, high-performance web framework
- **ASP.NET Core Web API** - RESTful API implementation
- **Microsoft Semantic Kernel 1.68.0** - LLM orchestration framework
- **Polly** - Resilience and retry policies for HTTP clients
- **OpenTelemetry** - Distributed tracing and metrics

### Frontend
- **React 18** - UI framework
- **Vite** - Build tool and dev server

### LLM Providers
- **Google Gemini 2.5 Flash Lite** - Fast, efficient model (default)
- **Google Gemini 2.5 Flash** - Balanced performance
- **OpenAI GPT-4o Mini** - Alternative model option

### External Services
- **OpenLibrary API** - Book metadata and cover images

### Infrastructure
- **Docker** - Containerization
- **Railway** - Backend hosting
- **Vercel** - Frontend hosting

## Project Structure

```
BookMatcher/
├── BookMatcher.Api/              # Web API project
│   ├── Controllers/              # API endpoints
│   ├── Program.cs                # App configuration
│   └── appsettings.json          # Configuration (API keys go here)
│
├── BookMatcher.Services/         # Business logic
│   ├── BookMatchService.cs       # Orchestration service
│   ├── LlmService.cs             # LLM interaction
│   ├── OpenLibraryService.cs     # OpenLibrary API client
│   └── Interfaces/               # Service contracts
│
├── BookMatcher.Common/           # Shared models
│   ├── Models/                   # DTOs and domain models
│   └── Exceptions/               # Custom exception types
│
├── BookMatcher.Tests/            # Unit tests
│   └── Services/                 # Service layer tests
│
├── frontend/                     # React frontend
│   ├── src/
│   │   ├── App.jsx               # Main component
│   │   ├── App.css               # Styles
│   │   └── main.jsx              # Entry point
│   ├── index.html                # HTML shell
│   └── vite.config.js            # Vite configuration
│
├── Dockerfile                    # Docker build
├── docker-compose.yml            # Local Docker setup
└── start.sh                      # Setup and run script
```

## API Reference

### Endpoint

```
GET /api/bookMatch/match
```

### Query Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| query | string | Yes | - | Natural language book query |
| model | int | No | 0 | LLM model (0=Gemini Flash Lite, 1=Gemini Flash, 2=GPT-4o Mini) |
| temperature | float | No | 0.7 | LLM creativity (0.0-1.0) |

### Response Format

```json
{
  "matches": [
    {
      "title": "The Hobbit",
      "primary_authors": ["J.R.R. Tolkien"],
      "contributors": [],
      "first_publish_year": 1937,
      "explanation": "This is an exact match. The subtitle of The Hobbit is 'There and Back Again'",
      "openlibrary_url": "https://openlibrary.org/works/OL262758W",
      "cover_url": "https://covers.openlibrary.org/b/id/12345-L.jpg"
    }
  ]
}
```

### Error Responses

| Status Code | Meaning |
|-------------|---------|
| 400 | Invalid query parameters |
| 404 | No matches found |
| 503 | LLM or OpenLibrary service unavailable |
| 500 | Unexpected server error |

### Example Requests

```bash
# Basic search
curl "http://localhost:5000/api/bookMatch/match?query=there%20and%20back%20again"

# With specific model
curl "http://localhost:5000/api/bookMatch/match?query=wizard%20school&model=2"

# With custom temperature
curl "http://localhost:5000/api/bookMatch/match?query=fellowship%20ring&temperature=0.5"
```

## Design Decisions

### Why Semantic Kernel?

Microsoft Semantic Kernel provides a clean abstraction over multiple LLM providers, allowing easy model switching and prompt management. This makes it easy to add new LLM providers in the future, as long as they have an SK Connector.

### Why Multiple LLM Models?

Two main reasons:
1. It is useful to be able to choose the LLM that best matches the use case. Different models have different strengths:
- **Gemini Flash Lite:** Fast and affordable (default)
- **Gemini Flash:** Better accuracy, slower
- **GPT-4o Mini:** Best accuracy, slowest

2. It is good to have fallback LLM models across different providers. If one provider's LLM API starts to fail, the backend can still process requests with another provider's models.


### Data Quality Considerations

- Multi-Stage Search Strategy: Implemented progressive query broadening to handle API strictness
  - Stage 1: Precise search with all fields (keywords + title + author)
  - Stage 2: Broader search with title + author only (if Stage 1 returns nothing)
  - Stage 3: Keywords-only search (if fewer than 5 results)
- Why: OpenLibrary API returns exact matches only; misspellings or overly specific queries can return nothing
- Field Normalization: Defensive normalization layer strips special characters, converts to lowercase, collapses spaces
  applied even though LLM should normalize (LLMs are unreliable)
- Primary Author Resolution: OpenLibrary often lists contributors (illustrators, editors) alongside primary authors. LLMs are tasked with distinguishing primary authors from contributors and justifying that in explanations
- De-duplication: Results de-duplicated by work key across all hypotheses to avoid showing same book multiple times
### Data Quality Issues Observed:
- First publish year occasionally incorrect (e.g., Harry Potter listed as published in 1900)
- Some works have cover_i, some don't (generated from work_key instead)
- Author name variations (J.R.R. vs JRR vs J R R Tolkien)
- LLMs will occasionally return exceedingly long, unrelated strings in fields that should be short and straightforward (i.e. title) 
### Error Handling Strategy

Custom exception types (LlmServiceException, OpenLibraryServiceException) allow specific HTTP status codes:
- 503 for service unavailability (retryable)
- 404 for no matches (expected behavior)
- 500 for unexpected errors (logging required)

## Observability

OpenTelemetry instrumentation provides:
- **Request + Response tracing:** End-to-end visibility of API calls
- **Token metrics:** LLM usage tracking for cost management
- **HTTP instrumentation:** Detailed logging of OpenLibrary and LLM API calls
- **Console export:** Easy debugging during development

Example token monitoring:
```json
  "usageMetadata": {
    "promptTokenCount": 457,
    "candidatesTokenCount": 61,
    "totalTokenCount": 518,
    "promptTokensDetails": [
      {
        "modality": "TEXT",
        "tokenCount": 457
      }
    ]
  },
  "modelVersion": "gemini-2.5-flash-lite",
  "responseId": "VUduafWFNo29_uMPiefkwAY"
```

## Testing Strategy

### Unit Tests

Located in `BookMatcher.Tests/Services/`:
- **OpenLibraryServiceTests:** API deserialization and error handling
- **LlmServiceTests:** Prompt generation and response parsing
- **BookMatchServiceTests:** End-to-end workflow orchestration

Run tests:
```bash
dotnet test
```

## Known Limitations

1. **Semantic Kernel Connectors**: Only a few LLMs are supported by official SK connectors currently, most of them are experimental
2. **OpenLibrary API Rate Limits:** No explicit rate limiting implemented (relies on Polly retry)
2. **Caching:** No result caching (every query hits LLM and OpenLibrary)
3. **Authentication:** No API key required (open endpoint)
4. **Input Validation:** Basic validation only (no profanity filter, etc.)

## Future Improvements

- Implement semantic result caching using Redis
- Implement request rate limiting
- Add integration tests with mocked LLM responses
- Connect OpenTelemetry to UI
  - Compare LLM responses in UI dashboard, like in LangSmith
- Add more LLM models (Claude, Llama)