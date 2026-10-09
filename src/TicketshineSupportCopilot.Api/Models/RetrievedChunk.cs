namespace TicketshineSupportCopilot.Api.Models;

public sealed record RetrievedChunk(
    long Id,
    string DocumentName,
    int ChunkIndex,
    string? Heading,
    string Content,
    double Distance);
