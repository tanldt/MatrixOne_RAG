using TicketshineSupportCopilot.Api.Models;
using TicketshineSupportCopilot.Api.Services;

namespace TicketshineSupportCopilot.Api.Tests;

public sealed class ChatServiceTests
{
    [Fact]
    public async Task AnswerAsync_NoIndexedChunks_ReturnsRunIngestionMessage()
    {
        var service = new ChatService(new FakeAiClient("unused"), new FakeMatrixOneRepository([]));

        var response = await service.AnswerAsync(new ChatRequest("Where are my tickets?"), CancellationToken.None);

        Assert.Contains("run ingestion", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(response.Citations);
    }

    [Fact]
    public async Task AnswerAsync_ReturnsCitationsFromRetrievedChunks()
    {
        var chunks = new[]
        {
            new RetrievedChunk(10, "refund-policy.md", 1, "Delayed ticket orders", "Refund rules.", 0.1)
        };
        var service = new ChatService(new FakeAiClient("Refunds depend on delivery failure."), new FakeMatrixOneRepository(chunks));

        var response = await service.AnswerAsync(new ChatRequest("When is a refund allowed?"), CancellationToken.None);

        Assert.Equal("Refunds depend on delivery failure.", response.Answer);
        var citation = Assert.Single(response.Citations);
        Assert.Equal("refund-policy.md", citation.DocumentName);
        Assert.Equal("Delayed ticket orders", citation.Heading);
        Assert.Equal(1, citation.ChunkIndex);
    }

    private sealed class FakeAiClient(string answer) : IAiClient
    {
        public Task<float[]> CreateEmbeddingAsync(string input, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Repeat(0.01f, 1536).ToArray());
        }

        public Task<string> CreateChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
        {
            return Task.FromResult(answer);
        }
    }

    private sealed class FakeMatrixOneRepository(IReadOnlyList<RetrievedChunk> chunks) : IMatrixOneRepository
    {
        public Task EnsureSchemaAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ResetDemoDataAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IndexedDocument> InsertDocumentWithChunksAsync(string name, string sourcePath, IReadOnlyList<(DocumentChunk Chunk, float[] Embedding)> chunks, CancellationToken cancellationToken)
        {
            return Task.FromResult(new IndexedDocument(1, name, sourcePath, chunks.Count));
        }

        public Task<IReadOnlyList<IndexedDocument>> ListDocumentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<IndexedDocument>>([]);
        }

        public Task<IReadOnlyList<RetrievedChunk>> SearchSimilarChunksAsync(float[] embedding, int limit, CancellationToken cancellationToken)
        {
            return Task.FromResult(chunks);
        }
    }
}
