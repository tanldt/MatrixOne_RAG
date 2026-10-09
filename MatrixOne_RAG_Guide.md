# MatrixOne và ứng dụng RAG — Hướng dẫn thực hành

## 1. MatrixOne là gì?

[MatrixOne](https://github.com/matrixorigin/matrixone) là cơ sở dữ liệu mã nguồn mở, hỗ trợ SQL và nhiều loại workload, bao gồm lưu trữ và tìm kiếm vector embeddings. MatrixOne **không phải** là LLM hoặc AI agent; trong hệ thống RAG, nó chủ yếu đóng vai trò lưu trữ tài liệu, metadata và truy xuất ngữ cảnh.

Các năng lực đáng chú ý:

- Relational SQL và analytical workloads.
- Vector data (`VECF32`), vector similarity search.
- Full-text search và khả năng kết hợp truy vấn theo từ khóa, vector, metadata.
- Tương thích giao thức MySQL (không có nghĩa tất cả tính năng MySQL/ORM hoàn toàn tương thích).

## 2. Có làm RAG được không?

**Có.** Luồng triển khai điển hình:

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

MatrixOne làm phần **storage và retrieval**. Bạn vẫn cần embedding model, chat model và phần ứng dụng điều phối pipeline.

## 3. Chạy thử bằng Docker

```bash
docker run -d \
  --name matrixone \
  -p 127.0.0.1:6001:6001 \
  matrixorigin/matrixone:latest

docker ps
```

Kết nối bằng MySQL client (tài khoản mặc định từ quick start, chỉ sử dụng local/demo; hãy đổi mật khẩu khi triển khai thực tế):

```bash
mysql -h 127.0.0.1 -P 6001 -u root -p111
```

## 4. Tạo bảng chunks và embeddings

Ví dụ dùng vector 1536 chiều, phù hợp với đầu ra mặc định của `text-embedding-3-small`:

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

> Lưu ý: Kiểm tra cú pháp với **phiên bản MatrixOne đang chạy**; số chiều trong cột vector phải khớp embedding model.

## 5. Tạo embeddings bằng Python

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

Sau đó insert `document_name`, `content` và **vector đầy đủ** vào MatrixOne thông qua driver tương thích MySQL. Đoạn code trên chỉ minh họa tạo embedding, chưa bao gồm bước insert.

## 6. Truy vấn vector similarity

Khi có câu hỏi mới, tạo embedding với **cùng embedding model**, sau đó truy vấn các chunks gần nhất:

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

`[0.012, -0.028, ...]` chỉ là placeholder: cần thay bằng vector hợp lệ, đủ số chiều và định dạng theo phiên bản MatrixOne đang dùng. Nên truyền tham số một cách an toàn thay vì ghép SQL từ input người dùng.

Với dataset lớn, xem xét IVF/HNSW vector index. Ví dụ IVF-FLAT (tính năng/cú pháp có thể phụ thuộc phiên bản):

```sql
SET experimental_ivf_index = 1;

CREATE INDEX idx_embedding
USING IVFFLAT
ON document_chunks(embedding)
LISTS = 100
OP_TYPE 'vector_l2_ops';
```

Với POC nhỏ, có thể bắt đầu bằng exact vector search để có baseline trước khi tối ưu index.

## 7. Sinh câu trả lời từ ngữ cảnh

Giả sử `retrieved_chunks` là danh sách nội dung truy vấn được từ MatrixOne:

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

Đây là minh họa riêng cho bước generation, không phải script end-to-end. Trong ứng dụng thật, nên truyền kèm tên tài liệu, section/page và ID chunk để LLM trích dẫn nguồn.

## 8. Kiến trúc đề xuất cho .NET / Azure

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

| Thành phần | Gợi ý |
|---|---|
| Frontend | Angular hoặc React |
| Backend | ASP.NET Core Web API |
| Database | MatrixOne |
| DB client | MySqlConnector (xác minh compatibility) |
| Embedding + LLM | Azure OpenAI |
| RAG orchestration | Microsoft Semantic Kernel hoặc custom C# |
| Background jobs | Hangfire |
| File storage | Azure Blob Storage |

### Project portfolio: Enterprise Knowledge Assistant

- Upload PDF, DOCX, Markdown.
- Extract text và chia thành chunks.
- Sinh embeddings và lưu vào MatrixOne.
- Search theo ngữ nghĩa, keyword và metadata.
- Chat hỏi đáp với source citations.
- Phân quyền truy cập tài liệu theo user/team.
- Theo dõi ingestion jobs, retries và chất lượng retrieval.

### Lộ trình

1. **Phase 1 — MVP:** Docker, nhập tài liệu, embeddings, vector search, chat endpoint.
2. **Phase 2 — Reliability:** background jobs, idempotency, retry, document update và citations.
3. **Phase 3 — Advanced RAG:** hybrid search, reranking, access controls, evaluation set.

## 9. So sánh nhanh

| Giải pháp | Phù hợp khi |
|---|---|
| **MatrixOne** | Muốn thử SQL + analytics + vector trong cùng engine |
| PostgreSQL + pgvector | Ứng dụng SQL truyền thống cần bổ sung RAG |
| Qdrant | Vector search chuyên biệt |
| Milvus | Vector search lớn, nhiều vector workloads |
| Azure AI Search | Managed search, hybrid retrieval trong Azure |

**Khuyến nghị:** MatrixOne đáng thử để học RAG và AI-native database. Với production .NET/Azure, nên benchmark thực tế với PostgreSQL + pgvector hoặc Azure AI Search về recall, latency, chi phí, vận hành và khả năng tích hợp.

## 10. Tài liệu chính thức

- [MatrixOne GitHub](https://github.com/matrixorigin/matrixone)
- [MatrixOne Documentation](https://docs.matrixorigin.cn/mo/en/)
- [RAG Application Demo](https://docs.matrixorigin.cn/mo/en/v26.4.1.0/MatrixOne/Tutorial/rag-demo.html)
- [Experimental IVF Index](https://docs.matrixorigin.cn/mo/en/v26.4.2.4/MatrixOne/Reference/Variable/system-variables/experimental_ivf_index.html)

---

*Ghi chú: Các ví dụ SQL và Docker là điểm khởi đầu để thử nghiệm. Cần đối chiếu tài liệu tương ứng với phiên bản MatrixOne cụ thể trước khi chạy production.*
