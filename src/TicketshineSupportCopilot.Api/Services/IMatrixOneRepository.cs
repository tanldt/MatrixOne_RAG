using TicketshineSupportCopilot.Api.Models;

namespace TicketshineSupportCopilot.Api.Services;

public interface IMatrixOneRepository
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken);

    Task ResetDemoDataAsync(CancellationToken cancellationToken);

    Task<IndexedDocument> InsertDocumentWithChunksAsync(
        string name,
        string sourcePath,
        IReadOnlyList<(DocumentChunk Chunk, float[] Embedding)> chunks,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<IndexedDocument>> ListDocumentsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<RetrievedChunk>> SearchSimilarChunksAsync(float[] embedding, int limit, CancellationToken cancellationToken);
}
