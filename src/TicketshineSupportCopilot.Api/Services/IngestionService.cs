using TicketshineSupportCopilot.Api.Models;

namespace TicketshineSupportCopilot.Api.Services;

public sealed class IngestionService(IAiClient aiClient, IMatrixOneRepository repository)
{
    public async Task<IngestionResult> RunAsync(CancellationToken cancellationToken)
    {
        var dataDirectory = FindSampleDataDirectory();
        var files = Directory.GetFiles(dataDirectory, "*.md").OrderBy(path => path).ToArray();
        var failedFiles = new List<string>();
        var documentCount = 0;
        var chunkCount = 0;

        await repository.EnsureSchemaAsync(cancellationToken);
        await repository.ResetDemoDataAsync(cancellationToken);

        foreach (var file in files)
        {
            try
            {
                var name = Path.GetFileName(file);
                var markdown = await File.ReadAllTextAsync(file, cancellationToken);
                var chunks = MarkdownChunker.Chunk(name, markdown);
                var embeddedChunks = new List<(DocumentChunk Chunk, float[] Embedding)>();

                foreach (var chunk in chunks)
                {
                    var embedding = await aiClient.CreateEmbeddingAsync(chunk.Content, cancellationToken);
                    embeddedChunks.Add((chunk, embedding));
                }

                await repository.InsertDocumentWithChunksAsync(name, file, embeddedChunks, cancellationToken);
                documentCount++;
                chunkCount += chunks.Count;
            }
            catch
            {
                failedFiles.Add(Path.GetFileName(file));
                throw;
            }
        }

        return new IngestionResult(documentCount, chunkCount, failedFiles);
    }

    private static string FindSampleDataDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "sample-data", "ticketshine");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find sample-data/ticketshine.");
    }
}
