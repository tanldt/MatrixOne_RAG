# MatrixOne and RAG Applications - Practical Guide

## 1. What Is MatrixOne?

[MatrixOne](https://github.com/matrixorigin/matrixone) is an open-source database that supports SQL and multiple workload types, including storing and searching vector embeddings. MatrixOne is **not** an LLM or an AI agent; in a RAG system, it mainly acts as the storage and retrieval layer for documents, metadata, and context.

Notable capabilities:

- Relational SQL and analytical workloads.
- Vector data (`VECF32`) and vector similarity search.
- Full-text search and the ability to combine keyword, vector, and metadata queries.
- MySQL protocol compatibility, though this does not mean every MySQL feature or ORM behavior is fully compatible.

## 2. Can It Be Used for RAG?

**Yes.** A typical implementation flow looks like this:

```text
PDF / DOCX / Web / Database
          |
   Extract + Chunking
          |
   Embedding Model
   (OpenAI/Azure OpenAI)
          |
        MatrixOne
 (chunks + metadata + vectors)
          ^
          | vector search / SQL filters
    User question -> Question embedding
          |
    Retrieved context
          |
     Chat LLM
          |
 Answer with source citations
```

MatrixOne handles the **storage and retrieval** part. You still need an embedding model, a chat model, and an application layer to orchestrate the pipeline.

## 3. Run It with Docker

```bash
docker run -d \
  --name matrixone \
  -p 127.0.0.1:6001:6001 \
  matrixorigin/matrixone:latest

docker ps
```

Connect with a MySQL client. The default quick-start account should only be used for local demos; change the password before any real deployment.

```bash
mysql -h 127.0.0.1 -P 6001 -u root -p111
```

## 4. Create a Chunks and Embeddings Table

This example uses a 1536-dimensional vector, matching the default output size of `text-embedding-3-small`:

```sql
CREATE DATABASE rag_demo;
USE rag_demo;

CREATE TABLE document_chunks (
    id BIGINT PRIMARY KEY,
    document_name VARCHAR(255),
    content TEXT,
    embedding VECF32(1536)
);
```

> Note: Check the syntax against the **specific MatrixOne version you are running**. The vector column dimension must match the embedding model output.

## 5. Create Embeddings with Python

```python
from openai import OpenAI

client = OpenAI()
text = (
    "How to troubleshoot a slow SQL query. "
    "Check execution plans, indexes and statistics."
)

response = client.embeddings.create(
    model="text-embedding-3-small",
    input=text,
)
embedding = response.data[0].embedding
print(len(embedding))  # 1536
```

After that, insert `document_name`, `content`, and the **full vector** into MatrixOne through a MySQL-compatible driver. The code above only demonstrates embedding generation; it does not include the insert step.

## 6. Query Vector Similarity

When a new question comes in, create an embedding with the **same embedding model**, then query the nearest chunks:

```sql
SELECT
    id,
    document_name,
    content,
    l2_distance(embedding, '[0.012, -0.028, ...]') AS distance
FROM document_chunks
ORDER BY distance ASC
LIMIT 5;
```

`[0.012, -0.028, ...]` is only a placeholder. Replace it with a valid vector that has the correct number of dimensions and the format expected by your MatrixOne version. Pass parameters safely instead of concatenating SQL from user input.

For larger datasets, consider IVF/HNSW vector indexes. Example IVF-FLAT syntax, depending on MatrixOne version and feature availability:

```sql
SET experimental_ivf_index = 1;

CREATE INDEX idx_embedding
USING IVFFLAT
ON document_chunks(embedding)
LISTS = 100
OP_TYPE 'vector_l2_ops';
```

For a small POC, start with exact vector search first so you have a baseline before optimizing indexes.

## 7. Generate an Answer from Retrieved Context

Assume `retrieved_chunks` is the list of content retrieved from MatrixOne:

```python
question = "How do I troubleshoot slow SQL queries?"
context = "\n\n".join(retrieved_chunks)

response = client.chat.completions.create(
    model="gpt-4.1-mini",
    messages=[
        {
            "role": "system",
            "content": (
                "Answer using only the provided context. "
                "If the answer is not in the context, say you don't know."
            ),
        },
        {
            "role": "user",
            "content": f"Context:\n{context}\n\nQuestion:\n{question}",
        },
    ],
)
print(response.choices[0].message.content)
```

This only illustrates the generation step; it is not an end-to-end script. In a real application, include document name, section/page, and chunk ID so the LLM can cite sources.

## 8. Suggested .NET / Azure Architecture

```text
React / Angular (Chat UI, upload)
              |
      ASP.NET Core Web API
      /                 \
Azure OpenAI          MatrixOne
(Embeddings + Chat)   (SQL + Vectors)
      \                 /
       RAG Orchestration
              |
    Hangfire/Worker Service
    (extract/chunk/index)
              |
       Azure Blob Storage
```

| Component | Suggestion |
|---|---|
| Frontend | Angular or React |
| Backend | ASP.NET Core Web API |
| Database | MatrixOne |
| DB client | MySqlConnector (verify compatibility) |
| Embedding + LLM | Azure OpenAI |
| RAG orchestration | Microsoft Semantic Kernel or custom C# |
| Background jobs | Hangfire |
| File storage | Azure Blob Storage |

### Portfolio Project: Enterprise Knowledge Assistant

- Upload PDF, DOCX, and Markdown files.
- Extract text and split it into chunks.
- Generate embeddings and store them in MatrixOne.
- Search by semantics, keywords, and metadata.
- Chat with source citations.
- Control document access by user/team.
- Track ingestion jobs, retries, and retrieval quality.

### Roadmap

1. **Phase 1 - MVP:** Docker, document ingestion, embeddings, vector search, chat endpoint.
2. **Phase 2 - Reliability:** background jobs, idempotency, retry, document updates, and citations.
3. **Phase 3 - Advanced RAG:** hybrid search, reranking, access controls, evaluation set.

### Demo in This Repo: Ticketshine Support Copilot

This repo extends the guide into a study demo that can also be used in a portfolio:

- Spec: `docs/superpowers/specs/2026-10-08-ticketshine-support-copilot-design.md`
- Backend: `src/TicketshineSupportCopilot.Api`
- Frontend: `web`
- Sample data: `sample-data/ticketshine`

Demo flow: run MatrixOne with Docker, run the ASP.NET Core API, run the React UI, click **Index docs**, then ask support questions about refunds, delayed orders, transfer emails, or escalation. The app retrieves chunks from MatrixOne and answers with citations.

## 9. Quick Comparison

| Solution | Best Fit |
|---|---|
| **MatrixOne** | You want to try SQL + analytics + vectors in the same engine |
| PostgreSQL + pgvector | A traditional SQL application needs RAG features |
| Qdrant | Specialized vector search |
| Milvus | Large-scale vector search and many vector workloads |
| Azure AI Search | Managed search and hybrid retrieval in Azure |

**Recommendation:** MatrixOne is worth trying for learning RAG and AI-native database patterns. For production .NET/Azure systems, benchmark against PostgreSQL + pgvector or Azure AI Search for recall, latency, cost, operations, and integration quality.

## 10. Official Documentation

- [MatrixOne GitHub](https://github.com/matrixorigin/matrixone)
- [MatrixOne Documentation](https://docs.matrixorigin.cn/mo/en/)
- [RAG Application Demo](https://docs.matrixorigin.cn/mo/en/v26.4.1.0/MatrixOne/Tutorial/rag-demo.html)
- [Experimental IVF Index](https://docs.matrixorigin.cn/mo/en/v26.4.2.4/MatrixOne/Reference/Variable/system-variables/experimental_ivf_index.html)

---

*Note: The SQL and Docker examples are starting points for experimentation. Always check the documentation for the specific MatrixOne version you are using before production deployment.*
