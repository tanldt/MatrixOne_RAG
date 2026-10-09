namespace TicketshineSupportCopilot.Api.Models;

public sealed record DocumentChunk(
    long Id,
    long DocumentId,
    string DocumentName,
    int ChunkIndex,
    string? Heading,
    string Content);
