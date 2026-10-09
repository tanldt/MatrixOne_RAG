using TicketshineSupportCopilot.Api.Models;
using TicketshineSupportCopilot.Api.Services;

namespace TicketshineSupportCopilot.Api.Tests;

public sealed class IngestionServiceTests
{
    [Fact]
    public async Task RunAsync_ReadsSampleDataChunksEmbedsAndStoresDocuments()
    {
        var ai = new FakeAiClient();
        var repository = new FakeMatrixOneRepository();
        var service = new IngestionService(ai, repository);

        var result = await service.RunAsync(CancellationToken.None);

        Assert.Equal(5, result.DocumentCount);
        Assert.True(result.ChunkCount >= 5);
        Assert.Empty(result.FailedFiles);
        Assert.Equal(5, repository.InsertedDocuments.Count);
    }

    [Fact]
    public async Task RunAsync_MissingApiCredentialsReturnsSetupError()
    {
        var ai = new ThrowingAiClient(new InvalidOperationException("Missing OPENAI_API_KEY."));
        var service = new IngestionService(ai, new FakeMatrixOneRepository());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RunAsync(CancellationToken.None));

        Assert.Contains("OPENAI_API_KEY", error.Message);
    }

    private sealed class FakeAiClient : IAiClient
    {
        public Task<float[]> CreateEmbeddingAsync(string input, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Repeat(0.01f, 1536).ToArray());
        }

        public Task<string> CreateChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
        {
            return Task.FromResult("ok");
        }
    }

    private sealed class ThrowingAiClient(Exception exception) : IAiClient
    {
        public Task<float[]> CreateEmbeddingAsync(string input, CancellationToken cancellationToken)
        {
            throw exception;
        }

        public Task<string> CreateChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
        {
            throw exception;
        }
    }

    private sealed class FakeMatrixOneRepository : IMatrixOneRepository
    {
        public List<string> InsertedDocuments { get; } = [];

        public Task EnsureSchemaAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ResetDemoDataAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IndexedDocument> InsertDocumentWithChunksAsync(
            string name,
            string sourcePath,
            IReadOnlyList<(DocumentChunk Chunk, float[] Embedding)> chunks,
            CancellationToken cancellationToken)
        {
            InsertedDocuments.Add(name);
            return Task.FromResult(new IndexedDocument(InsertedDocuments.Count, name, sourcePath, chunks.Count));
        }

        public Task<IReadOnlyList<IndexedDocument>> ListDocumentsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<IndexedDocument>>([]);
        }

        public Task<IReadOnlyList<RetrievedChunk>> SearchSimilarChunksAsync(float[] embedding, int limit, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<RetrievedChunk>>([]);
        }
    }
}
