# Ticketshine Support Copilot

Local RAG demo using ASP.NET Core, React, OpenAI, and MatrixOne. The app ingests synthetic Ticketshine-style support documents, stores chunk embeddings in MatrixOne, and answers support questions with source citations.

## Prerequisites

- .NET 9 SDK
- Node.js 22
- Docker
- OpenAI API key

## Configuration

Use the values in `.env.example` as your local checklist:

```powershell
$env:OPENAI_API_KEY="your-key"
$env:OPENAI_EMBEDDING_MODEL="text-embedding-3-small"
$env:OPENAI_CHAT_MODEL="gpt-4.1-mini"
$env:MATRIXONE_HOST="127.0.0.1"
$env:MATRIXONE_PORT="6001"
$env:MATRIXONE_USER="root"
$env:MATRIXONE_PASSWORD="111"
$env:MATRIXONE_DATABASE="ticketshine_rag"
```

Do not commit secrets.

## Run The Demo

Start MatrixOne:

```powershell
docker compose up -d
```

Run the API:

```powershell
dotnet run --project src/TicketshineSupportCopilot.Api --launch-profile http
```

Run the frontend:

```powershell
cd web
npm install
npm run dev
```

Open `http://127.0.0.1:5173/`, click **Index docs**, then ask one of the sample questions.

## Sample Questions

- A customer says their ticket transfer email never arrived. What should I check first?
- When is a refund allowed for a delayed ticket order?
- Write a short reply for a customer whose order is delayed but still expected to be delivered.
- When should support escalate an order issue to operations?

## Verification

Backend tests:

```powershell
dotnet test
```

Frontend build:

```powershell
cd web
npm run build
```
