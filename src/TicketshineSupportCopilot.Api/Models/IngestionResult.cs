namespace TicketshineSupportCopilot.Api.Models;

public sealed record IngestionResult(int DocumentCount, int ChunkCount, IReadOnlyList<string> FailedFiles);
