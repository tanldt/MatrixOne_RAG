# Ticketshine Support Copilot Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a local RAG support copilot demo that ingests synthetic Ticketshine support docs, stores embeddings in MatrixOne, and answers support questions with citations.

**Architecture:** A React + Vite frontend calls an ASP.NET Core Web API. The API owns ingestion, chunking, embedding, MatrixOne persistence/retrieval, prompt building, and grounded chat responses. MatrixOne runs locally through Docker and stores documents, chunks, metadata, and `VECF32(1536)` embeddings.

**Tech Stack:** .NET 9.0, ASP.NET Core Web API, xUnit, MySqlConnector, OpenAI HTTP API, React, Vite, TypeScript, MatrixOne Docker.

**Spec:** `docs/superpowers/specs/2026-10-08-ticketshine-support-copilot-design.md`

## Global Constraints

- Use synthetic Ticketshine-style documents only; do not include real Ticketshine production data.
- MVP endpoints are `POST /api/ingestion/run`, `POST /api/chat`, and `GET /api/documents`.
- Use `text-embedding-3-small` with 1536-dimensional embeddings by default.
- Use exact vector search with `l2_distance` for the MVP; do not add IVF/HNSW indexes yet.
- Answers must be grounded in retrieved context and include citations with document name, heading when available, and chunk index.
- Do not commit secrets; read OpenAI and MatrixOne settings from configuration or environment variables.
- If MatrixOne is unavailable, return a clear setup error telling the user to start Docker.
- If AI credentials are missing, return a setup error.
- If no chunks are indexed, tell the user to run ingestion first.

## Review Focus

- Missing AI credentials should produce a clear API error, covered in Task 5.
- No indexed chunks should produce an actionable chat response error, covered in Task 6.
- Markdown with repeated or empty headings should still produce useful chunk metadata, covered in Task 2.
- MatrixOne SQL should not concatenate raw user questions into queries, covered in Task 4.
- UI text should not overlap or overflow on desktop/mobile widths, covered in Task 7.

---

## File Structure

- `TicketshineSupportCopilot.sln`: .NET solution.
- `src/TicketshineSupportCopilot.Api/`: ASP.NET Core API project.
- `src/TicketshineSupportCopilot.Api/Models/`: request, response, domain, and configuration records.
- `src/TicketshineSupportCopilot.Api/Services/`: chunking, ingestion, AI, MatrixOne, retrieval, prompt, and chat orchestration services.
- `src/TicketshineSupportCopilot.Api/Data/`: MatrixOne schema SQL and data-access helpers.
- `tests/TicketshineSupportCopilot.Api.Tests/`: xUnit tests for backend services and endpoints.
- `web/`: React + Vite + TypeScript frontend.
- `sample-data/ticketshine/`: synthetic Markdown support documents.
- `docker-compose.yml`: MatrixOne local dependency.
- `.env.example`: documented local environment variables without secrets.
- `README.md`: setup, run, ingest, and demo instructions.

## Task 1: Project Scaffold And Local Runtime

**Files:**
- Create: `TicketshineSupportCopilot.sln`
- Create: `src/TicketshineSupportCopilot.Api/TicketshineSupportCopilot.Api.csproj`
- Create: `src/TicketshineSupportCopilot.Api/Program.cs`
- Create: `src/TicketshineSupportCopilot.Api/appsettings.json`
- Create: `tests/TicketshineSupportCopilot.Api.Tests/TicketshineSupportCopilot.Api.Tests.csproj`
- Create: `docker-compose.yml`
- Create: `.env.example`
- Create: `README.md`

**Interfaces:**
- Produces: ASP.NET Core app with health endpoint `GET /health` returning `{ "status": "ok" }`.
- Produces: solution containing API and test projects.

- [ ] **Step 1: Scaffold solution and projects**

Run:

```powershell
dotnet new sln -n TicketshineSupportCopilot
dotnet new webapi -n TicketshineSupportCopilot.Api -o src/TicketshineSupportCopilot.Api
dotnet new xunit -n TicketshineSupportCopilot.Api.Tests -o tests/TicketshineSupportCopilot.Api.Tests
dotnet sln add src/TicketshineSupportCopilot.Api/TicketshineSupportCopilot.Api.csproj
dotnet sln add tests/TicketshineSupportCopilot.Api.Tests/TicketshineSupportCopilot.Api.Tests.csproj
dotnet add tests/TicketshineSupportCopilot.Api.Tests/TicketshineSupportCopilot.Api.Tests.csproj reference src/TicketshineSupportCopilot.Api/TicketshineSupportCopilot.Api.csproj
dotnet add tests/TicketshineSupportCopilot.Api.Tests/TicketshineSupportCopilot.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
```

Expected: solution builds with generated projects.

- [ ] **Step 2: Add failing health endpoint test**

Create `tests/TicketshineSupportCopilot.Api.Tests/HealthEndpointTests.cs` with a `GetHealth_ReturnsOkStatus` test using `WebApplicationFactory<Program>` and asserting JSON contains `status = "ok"`.

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/TicketshineSupportCopilot.Api.Tests/TicketshineSupportCopilot.Api.Tests.csproj --filter GetHealth_ReturnsOkStatus`

Expected: FAIL because the endpoint/test host setup is not implemented.

- [ ] **Step 4: Implement minimal API health endpoint**

Modify `Program.cs` to expose `GET /health` and make `Program` public partial for integration tests. Remove generated weather sample code.

- [ ] **Step 5: Add local runtime files**

Create `docker-compose.yml` with MatrixOne service `matrixone`, image `matrixorigin/matrixone:latest`, and port `127.0.0.1:6001:6001`. Create `.env.example` with `OPENAI_API_KEY=`, `OPENAI_EMBEDDING_MODEL=text-embedding-3-small`, `OPENAI_CHAT_MODEL=gpt-4.1-mini`, `MATRIXONE_HOST=127.0.0.1`, `MATRIXONE_PORT=6001`, `MATRIXONE_USER=root`, `MATRIXONE_PASSWORD=111`, `MATRIXONE_DATABASE=ticketshine_rag`.

- [ ] **Step 6: Run verification**

Run: `dotnet test`

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add TicketshineSupportCopilot.sln src tests docker-compose.yml .env.example README.md
git commit -m "chore: scaffold support copilot demo"
```

Skip commit if the workspace is not a git repository.

## Task 2: Sample Data And Markdown Chunking

**Files:**
- Create: `sample-data/ticketshine/refund-policy.md`
- Create: `sample-data/ticketshine/delayed-orders-playbook.md`
- Create: `sample-data/ticketshine/ticket-transfer-guide.md`
- Create: `sample-data/ticketshine/support-escalation-sop.md`
- Create: `sample-data/ticketshine/customer-message-templates.md`
- Create: `src/TicketshineSupportCopilot.Api/Models/DocumentChunk.cs`
- Create: `src/TicketshineSupportCopilot.Api/Services/MarkdownChunker.cs`
- Test: `tests/TicketshineSupportCopilot.Api.Tests/MarkdownChunkerTests.cs`

**Interfaces:**
- Produces: `record DocumentChunk(long Id, long DocumentId, string DocumentName, int ChunkIndex, string? Heading, string Content);`
- Produces: `MarkdownChunker.Chunk(string documentName, string markdown, int maxCharacters = 1200) -> IReadOnlyList<DocumentChunk>`

- [ ] **Step 1: Add failing chunking tests**

Create tests:

- `Chunk_KeepsHeadingMetadata` asserts content under `## Missing transfer email` has heading `Missing transfer email`.
- `Chunk_SplitsLongSectionByMaxCharacters` asserts a long section creates more than one chunk, each `Content.Length <= 1200`.
- `Chunk_RepeatedOrEmptyHeadingsStillProducesUsefulMetadata` asserts repeated headings keep non-empty heading text and empty heading lines fall back to the previous useful heading or document name.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter MarkdownChunkerTests`

Expected: FAIL because `MarkdownChunker` does not exist.

- [ ] **Step 3: Implement `DocumentChunk` and `MarkdownChunker`**

Implement heading-aware Markdown chunking. Assign `DocumentId = 0` and `Id = 0` in chunker output; persistence assigns real IDs later. Use 1-based `ChunkIndex`.

- [ ] **Step 4: Add synthetic sample documents**

Create the five Markdown files named in the spec. Include support-specific sections that answer the example questions from the spec.

- [ ] **Step 5: Run verification**

Run: `dotnet test --filter MarkdownChunkerTests`

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add sample-data src/TicketshineSupportCopilot.Api/Models/DocumentChunk.cs src/TicketshineSupportCopilot.Api/Services/MarkdownChunker.cs tests/TicketshineSupportCopilot.Api.Tests/MarkdownChunkerTests.cs
git commit -m "feat: add sample support docs and chunking"
```

Skip commit if the workspace is not a git repository.

## Task 3: Configuration And AI Client

**Files:**
- Create: `src/TicketshineSupportCopilot.Api/Models/AiOptions.cs`
- Create: `src/TicketshineSupportCopilot.Api/Services/IAiClient.cs`
- Create: `src/TicketshineSupportCopilot.Api/Services/OpenAiClient.cs`
- Test: `tests/TicketshineSupportCopilot.Api.Tests/OpenAiClientConfigurationTests.cs`

**Interfaces:**
- Produces: `record AiOptions(string ApiKey, string EmbeddingModel, string ChatModel);`
- Produces: `IAiClient.CreateEmbeddingAsync(string input, CancellationToken cancellationToken) -> Task<float[]>`
- Produces: `IAiClient.CreateChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken) -> Task<string>`

- [ ] **Step 1: Add failing configuration tests**

Create tests:

- `AiOptions_MissingApiKey_IsInvalid` asserts missing `OPENAI_API_KEY` maps to a validation failure message containing `OPENAI_API_KEY`.
- `AiOptions_DefaultModels_AreSpecDefaults` asserts defaults are `text-embedding-3-small` and `gpt-4.1-mini`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter OpenAiClientConfigurationTests`

Expected: FAIL because options/client do not exist.

- [ ] **Step 3: Implement options and client interface**

Bind AI settings from configuration/environment. Validate API key before making HTTP calls.

- [ ] **Step 4: Implement `OpenAiClient`**

Use `HttpClient` to call OpenAI embeddings and chat completions endpoints. Parse embedding arrays as `float[]` and chat text as `string`. Keep request DTOs internal to the service.

- [ ] **Step 5: Register services**

Modify `Program.cs` to register `IAiClient`, `OpenAiClient`, `HttpClient`, and options.

- [ ] **Step 6: Run verification**

Run: `dotnet test --filter OpenAiClientConfigurationTests`

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/TicketshineSupportCopilot.Api tests/TicketshineSupportCopilot.Api.Tests/OpenAiClientConfigurationTests.cs
git commit -m "feat: add OpenAI client configuration"
```

Skip commit if the workspace is not a git repository.

## Task 4: MatrixOne Schema And Repository

**Files:**
- Create: `src/TicketshineSupportCopilot.Api/Models/MatrixOneOptions.cs`
- Create: `src/TicketshineSupportCopilot.Api/Models/IndexedDocument.cs`
- Create: `src/TicketshineSupportCopilot.Api/Models/RetrievedChunk.cs`
- Create: `src/TicketshineSupportCopilot.Api/Data/schema.sql`
- Create: `src/TicketshineSupportCopilot.Api/Services/IMatrixOneRepository.cs`
- Create: `src/TicketshineSupportCopilot.Api/Services/MatrixOneRepository.cs`
- Test: `tests/TicketshineSupportCopilot.Api.Tests/MatrixOneRepositorySqlTests.cs`

**Interfaces:**
- Produces: `record MatrixOneOptions(string Host, int Port, string User, string Password, string Database);`
- Produces: `record IndexedDocument(long Id, string Name, string SourcePath, int ChunkCount);`
- Produces: `record RetrievedChunk(long Id, string DocumentName, int ChunkIndex, string? Heading, string Content, double Distance);`
- Produces: `IMatrixOneRepository.EnsureSchemaAsync(CancellationToken) -> Task`
- Produces: `IMatrixOneRepository.ResetDemoDataAsync(CancellationToken) -> Task`
- Produces: `IMatrixOneRepository.InsertDocumentWithChunksAsync(string name, string sourcePath, IReadOnlyList<(DocumentChunk Chunk, float[] Embedding)> chunks, CancellationToken) -> Task<IndexedDocument>`
- Produces: `IMatrixOneRepository.ListDocumentsAsync(CancellationToken) -> Task<IReadOnlyList<IndexedDocument>>`
- Produces: `IMatrixOneRepository.SearchSimilarChunksAsync(float[] embedding, int limit, CancellationToken) -> Task<IReadOnlyList<RetrievedChunk>>`

- [ ] **Step 1: Add failing SQL safety tests**

Create tests:

- `SearchSimilarChunks_UsesParameterizedLimitAndDoesNotUseQuestionText` inspects repository command-building helper and asserts no raw user question parameter exists.
- `VectorLiteral_FormatsInvariantCulture` asserts vector values use `.` decimal separator and bracket syntax like `[0.1,-0.2]`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter MatrixOneRepositorySqlTests`

Expected: FAIL because repository does not exist.

- [ ] **Step 3: Add MatrixOne schema SQL**

Create `schema.sql` matching the spec's `documents` and `document_chunks` tables with `embedding VECF32(1536)`.

- [ ] **Step 4: Implement repository**

Add package `MySqlConnector` to the API project. Use parameterized commands for string and numeric values. Convert embeddings to MatrixOne vector literal with invariant culture in one helper: `MatrixOneRepository.FormatVectorLiteral(float[] embedding) -> string`.

- [ ] **Step 5: Register repository**

Modify `Program.cs` to register `IMatrixOneRepository` and `MatrixOneOptions`.

- [ ] **Step 6: Run verification**

Run: `dotnet test --filter MatrixOneRepositorySqlTests`

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/TicketshineSupportCopilot.Api tests/TicketshineSupportCopilot.Api.Tests/MatrixOneRepositorySqlTests.cs
git commit -m "feat: add MatrixOne repository"
```

Skip commit if the workspace is not a git repository.

## Task 5: Ingestion Endpoint

**Files:**
- Create: `src/TicketshineSupportCopilot.Api/Models/IngestionResult.cs`
- Create: `src/TicketshineSupportCopilot.Api/Services/IngestionService.cs`
- Test: `tests/TicketshineSupportCopilot.Api.Tests/IngestionServiceTests.cs`
- Modify: `src/TicketshineSupportCopilot.Api/Program.cs`

**Interfaces:**
- Produces: `record IngestionResult(int DocumentCount, int ChunkCount, IReadOnlyList<string> FailedFiles);`
- Produces: `IngestionService.RunAsync(CancellationToken cancellationToken) -> Task<IngestionResult>`
- Produces: `POST /api/ingestion/run -> IngestionResult`
- Produces: `GET /api/documents -> IReadOnlyList<IndexedDocument>`

- [ ] **Step 1: Add failing ingestion tests**

Create tests:

- `RunAsync_ReadsSampleDataChunksEmbedsAndStoresDocuments` uses fake `IAiClient` and fake `IMatrixOneRepository`, then asserts five documents are processed.
- `RunAsync_MissingApiCredentialsReturnsSetupError` asserts missing API key is surfaced as a setup error containing `OPENAI_API_KEY`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter IngestionServiceTests`

Expected: FAIL because ingestion service does not exist.

- [ ] **Step 3: Implement `IngestionService`**

Read `sample-data/ticketshine/*.md`, chunk each file, embed each chunk, call repository schema/reset/insert methods, and return document/chunk counts.

- [ ] **Step 4: Add endpoints**

Map `POST /api/ingestion/run` and `GET /api/documents` in `Program.cs`. Convert setup and MatrixOne connection failures into clear `ProblemDetails` responses.

- [ ] **Step 5: Run verification**

Run: `dotnet test --filter IngestionServiceTests`

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/TicketshineSupportCopilot.Api tests/TicketshineSupportCopilot.Api.Tests/IngestionServiceTests.cs
git commit -m "feat: add document ingestion endpoint"
```

Skip commit if the workspace is not a git repository.

## Task 6: Retrieval, Prompting, And Chat Endpoint

**Files:**
- Create: `src/TicketshineSupportCopilot.Api/Models/ChatRequest.cs`
- Create: `src/TicketshineSupportCopilot.Api/Models/ChatResponse.cs`
- Create: `src/TicketshineSupportCopilot.Api/Models/Citation.cs`
- Create: `src/TicketshineSupportCopilot.Api/Services/PromptBuilder.cs`
- Create: `src/TicketshineSupportCopilot.Api/Services/ChatService.cs`
- Test: `tests/TicketshineSupportCopilot.Api.Tests/PromptBuilderTests.cs`
- Test: `tests/TicketshineSupportCopilot.Api.Tests/ChatServiceTests.cs`
- Modify: `src/TicketshineSupportCopilot.Api/Program.cs`

**Interfaces:**
- Produces: `record ChatRequest(string Question);`
- Produces: `record Citation(string DocumentName, string? Heading, int ChunkIndex);`
- Produces: `record ChatResponse(string Answer, IReadOnlyList<Citation> Citations);`
- Produces: `PromptBuilder.Build(string question, IReadOnlyList<RetrievedChunk> chunks) -> (string SystemPrompt, string UserPrompt)`
- Produces: `ChatService.AnswerAsync(ChatRequest request, CancellationToken cancellationToken) -> Task<ChatResponse>`
- Produces: `POST /api/chat -> ChatResponse`

- [ ] **Step 1: Add failing prompt tests**

Create tests:

- `Build_IncludesCitationMetadata` asserts document name, heading, and chunk index appear in the prompt.
- `Build_InstructsModelToUseOnlyProvidedContext` asserts system prompt includes "Answer only from the provided context".

- [ ] **Step 2: Add failing chat service tests**

Create tests:

- `AnswerAsync_NoIndexedChunks_ReturnsRunIngestionMessage` uses fake repository returning no chunks and asserts answer tells user to run ingestion first.
- `AnswerAsync_ReturnsCitationsFromRetrievedChunks` uses fake retrieved chunks and fake AI answer, then asserts citations mirror retrieved chunk metadata.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test --filter "PromptBuilderTests|ChatServiceTests"`

Expected: FAIL because prompt/chat services do not exist.

- [ ] **Step 4: Implement `PromptBuilder`**

Build compact prompts with chunk IDs, document names, headings, chunk indexes, and content. Include exact grounded-answer rules from the spec.

- [ ] **Step 5: Implement `ChatService`**

Create question embedding, retrieve top 5 chunks, handle empty results, build prompt, call `IAiClient.CreateChatCompletionAsync`, and map citations.

- [ ] **Step 6: Add chat endpoint**

Map `POST /api/chat` in `Program.cs`. Validate non-empty `Question`.

- [ ] **Step 7: Run verification**

Run: `dotnet test --filter "PromptBuilderTests|ChatServiceTests"`

Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add src/TicketshineSupportCopilot.Api tests/TicketshineSupportCopilot.Api.Tests/PromptBuilderTests.cs tests/TicketshineSupportCopilot.Api.Tests/ChatServiceTests.cs
git commit -m "feat: add grounded chat endpoint"
```

Skip commit if the workspace is not a git repository.

## Task 7: Frontend App

**Files:**
- Create: `web/package.json`
- Create: `web/vite.config.ts`
- Create: `web/tsconfig.json`
- Create: `web/index.html`
- Create: `web/src/main.tsx`
- Create: `web/src/App.tsx`
- Create: `web/src/api.ts`
- Create: `web/src/App.css`

**Interfaces:**
- Consumes: `GET /api/documents`, `POST /api/ingestion/run`, `POST /api/chat`.
- Produces: Support copilot UI with chat transcript, question input, citations panel, and indexed document status.

- [ ] **Step 1: Scaffold Vite React app**

Run:

```powershell
npm create vite@latest web -- --template react-ts
cd web
npm install
```

Expected: React TypeScript app exists.

- [ ] **Step 2: Implement API client**

Create `api.ts` with `getDocuments()`, `runIngestion()`, and `sendChat(question: string)` functions using relative `/api` URLs.

- [ ] **Step 3: Implement usable copilot screen**

Modify `App.tsx` to render document status, ingest button, chat transcript, question input, send button, and citations panel. Include example prompt buttons for the four spec questions.

- [ ] **Step 4: Style as internal support tool**

Modify `App.css` for responsive layout. Use restrained colors, fixed toolbar/button sizing, and a mobile layout that stacks citations below chat.

- [ ] **Step 5: Run verification**

Run:

```powershell
cd web
npm run build
```

Expected: PASS.

- [ ] **Step 6: Check responsive layout**

Run `npm run dev -- --host 127.0.0.1`, open the app at desktop width around 1440px and mobile width around 390px, and confirm the chat controls, example prompt buttons, document status, and citations panel do not overlap or overflow.

- [ ] **Step 7: Commit**

```powershell
git add web
git commit -m "feat: add support copilot frontend"
```

Skip commit if the workspace is not a git repository.

## Task 8: End-To-End Wiring And Documentation

**Files:**
- Modify: `src/TicketshineSupportCopilot.Api/Program.cs`
- Modify: `src/TicketshineSupportCopilot.Api/TicketshineSupportCopilot.Api.csproj`
- Modify: `README.md`
- Modify: `MatrixOne_RAG_Guide.md`

**Interfaces:**
- Consumes: all previous backend and frontend interfaces.
- Produces: documented local demo run flow.

- [ ] **Step 1: Configure frontend dev proxy**

Set Vite dev server proxy for `/api` and `/health` to the ASP.NET Core API local URL.

- [ ] **Step 2: Enable API CORS for local frontend**

Configure CORS for `http://localhost:5173` in `Program.cs`.

- [ ] **Step 3: Update README**

Document:

- prerequisites: .NET 9, Node 22, Docker, OpenAI API key
- `docker compose up -d`
- `dotnet run --project src/TicketshineSupportCopilot.Api`
- `npm install` and `npm run dev` from `web`
- run ingestion
- ask sample questions

- [ ] **Step 4: Link the new demo from guide**

Add a short section to `MatrixOne_RAG_Guide.md` pointing to the Ticketshine Support Copilot spec and local demo flow.

- [ ] **Step 5: Run full verification**

Run:

```powershell
dotnet test
cd web
npm run build
```

Expected: PASS.

- [ ] **Step 6: Manual smoke test**

With Docker and API credentials available:

```powershell
docker compose up -d
dotnet run --project src/TicketshineSupportCopilot.Api
```

In a second terminal:

```powershell
cd web
npm run dev
```

Open the Vite URL, run ingestion, ask "When is a refund allowed for a delayed ticket order?", and confirm the answer includes at least one citation from `refund-policy.md` or `delayed-orders-playbook.md`.

- [ ] **Step 7: Commit**

```powershell
git add README.md MatrixOne_RAG_Guide.md src/TicketshineSupportCopilot.Api web
git commit -m "docs: document local RAG demo flow"
```

Skip commit if the workspace is not a git repository.
