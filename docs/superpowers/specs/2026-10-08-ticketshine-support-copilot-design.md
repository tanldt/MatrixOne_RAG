# Ticketshine Support Copilot - Design Spec

## Purpose

Build a focused RAG demo that shows how MatrixOne can power a customer support copilot for Ticketshine-style workflows. The demo should help a support agent ask natural-language questions about order issues, refund rules, transfer procedures, and delayed-order handling, then receive a grounded answer with source citations.

The project is primarily for study, but it should also be presentable on tanle.ca as a portfolio case study. The first version should stay small enough to build and understand end to end.

## Success Criteria

- A user can ingest a small set of Markdown support documents.
- The app chunks documents, generates embeddings, and stores chunks plus vectors in MatrixOne.
- A user can ask support questions through a chat interface.
- The backend retrieves relevant chunks from MatrixOne and generates an answer using only retrieved context.
- Answers include source citations with document name and chunk or section reference.
- The demo can be explained clearly as a support workflow, not only as a technical experiment.

## Non-Goals

- No real Ticketshine production data in the MVP.
- No customer authentication or full role-based access control in the MVP.
- No production-grade job queue, retries, or document versioning in the MVP.
- No claim that MatrixOne is the final production choice before benchmarking.

## Demo Scenario

The demo uses synthetic Ticketshine-style documents:

- `refund-policy.md`
- `delayed-orders-playbook.md`
- `ticket-transfer-guide.md`
- `support-escalation-sop.md`
- `customer-message-templates.md`

Example questions:

- "A customer says their ticket transfer email never arrived. What should I check first?"
- "When is a refund allowed for a delayed ticket order?"
- "Write a short reply for a customer whose order is delayed but still expected to be delivered."
- "When should support escalate an order issue to operations?"

The app should answer with a concise response and cite the supporting document chunks.

## Architecture

```text
React Chat UI
      |
ASP.NET Core Web API
      |
RAG Orchestrator
  |        |
  |        +-- OpenAI or Azure OpenAI
  |            - embeddings
  |            - chat completion
  |
  +-- MatrixOne
      - documents
      - document_chunks
      - vector similarity search

Local Markdown Docs
      |
Ingestion CLI or API endpoint
      |
Extract -> Chunk -> Embed -> Store
```

## Components

### Frontend

Use a simple React app for the portfolio demo. The first screen should be the usable support copilot, not a landing page.

Core UI:

- Chat transcript.
- Question input.
- Answer citations panel.
- Small document status area showing which demo documents were indexed.

The UI should feel like an internal support tool: practical, scannable, and professional.

### Backend API

Use ASP.NET Core Web API.

Initial endpoints:

- `POST /api/ingestion/run`
  - Reads local demo documents.
  - Chunks and indexes them.
  - Safe to rerun for the MVP by clearing/replacing demo chunks.
- `POST /api/chat`
  - Accepts a user question.
  - Creates a question embedding.
  - Retrieves top matching chunks from MatrixOne.
  - Calls the chat model with a grounded prompt.
  - Returns answer plus citations.
- `GET /api/documents`
  - Returns indexed document names and chunk counts.

### MatrixOne Schema

Use a minimal schema first:

```sql
CREATE DATABASE IF NOT EXISTS ticketshine_rag;
USE ticketshine_rag;

CREATE TABLE IF NOT EXISTS documents (
    id BIGINT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    source_path VARCHAR(500) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS document_chunks (
    id BIGINT PRIMARY KEY,
    document_id BIGINT NOT NULL,
    document_name VARCHAR(255) NOT NULL,
    chunk_index INT NOT NULL,
    heading VARCHAR(255),
    content TEXT NOT NULL,
    embedding VECF32(1536),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

For the MVP, exact vector search is enough. IVF/HNSW indexing can be added later after the retrieval baseline works.

### Ingestion

The ingestion flow should be intentionally simple:

1. Read Markdown files from a local `sample-data/ticketshine/` folder.
2. Split text by headings and size-aware chunks.
3. Generate embeddings with `text-embedding-3-small` or the Azure OpenAI equivalent.
4. Store document rows and chunk rows in MatrixOne.
5. Return chunk counts and any failed files.

Rerunning ingestion in the MVP can reset demo tables. Later versions can add stable IDs and idempotency.

### Retrieval

For each question:

1. Generate an embedding using the same embedding model used for chunks.
2. Query MatrixOne for the closest chunks using `l2_distance`.
3. Return the top 5 chunks with metadata.
4. Build a compact prompt that includes chunk IDs, document names, headings, and content.

The system prompt should require grounded answers:

- Answer only from the provided context.
- If the answer is not available, say what is missing.
- Cite sources by document name and chunk number.
- Keep support replies practical and agent-ready.

### Citations

Each citation should include:

- document name
- heading when available
- chunk index

Example:

```json
{
  "answer": "Check whether the transfer email was generated, whether the recipient email is correct, and whether the ticket provider has a transfer delay.",
  "citations": [
    {
      "documentName": "ticket-transfer-guide.md",
      "heading": "Missing transfer email",
      "chunkIndex": 2
    }
  ]
}
```

## Configuration

Use environment variables or local app settings for:

- `OPENAI_API_KEY` or Azure OpenAI credentials.
- `OPENAI_EMBEDDING_MODEL`
- `OPENAI_CHAT_MODEL`
- `MATRIXONE_HOST`
- `MATRIXONE_PORT`
- `MATRIXONE_USER`
- `MATRIXONE_PASSWORD`
- `MATRIXONE_DATABASE`

Do not commit secrets.

## Error Handling

MVP behavior:

- If MatrixOne is unavailable, return a clear API error telling the user to start Docker.
- If AI credentials are missing, return a setup error.
- If no chunks are indexed, tell the user to run ingestion first.
- If retrieval returns weak or empty context, the assistant should say it does not have enough information.

## Testing

Initial tests should cover:

- Markdown chunking keeps useful headings.
- Ingestion creates expected document and chunk records.
- Chat endpoint handles no indexed documents.
- Prompt builder includes citation metadata.
- Retrieval result mapping returns citations in the API response.

Manual verification:

- Start MatrixOne in Docker.
- Run ingestion.
- Ask 3-5 sample support questions.
- Confirm answers cite the expected synthetic source docs.

## Portfolio Presentation

The portfolio write-up should describe:

- Problem: support agents need fast, source-grounded answers.
- Solution: RAG support copilot using MatrixOne vector search and .NET.
- Technical stack: React, ASP.NET Core, MatrixOne, OpenAI/Azure OpenAI.
- Demo flow: ingest support docs, ask questions, answer with citations.
- Lessons learned: retrieval quality, chunking, citations, database tradeoffs.

Screenshots should show the chat UI, citations panel, and indexed document list.

## Future Phases

### Phase 2 - Reliability

- Background ingestion jobs.
- Idempotent document updates.
- Better chunk IDs.
- Retry handling.
- Basic admin view for ingestion status.

### Phase 3 - Advanced RAG

- Hybrid keyword and vector search.
- Reranking.
- Evaluation question set.
- Retrieval quality metrics.
- Access control by team or document group.

### Phase 4 - Production Comparison

Benchmark MatrixOne against PostgreSQL with pgvector and Azure AI Search for:

- recall
- latency
- operational complexity
- cost
- .NET integration quality

## Open Decisions

- Use OpenAI directly or Azure OpenAI for the first local demo.
- Use React or Angular for frontend. React is recommended for faster portfolio delivery.
- Keep ingestion as an API endpoint or make it a separate CLI. API endpoint is recommended for a smoother demo.
