namespace TicketshineSupportCopilot.Api.Models;

public sealed record IndexedDocument(long Id, string Name, string SourcePath, int ChunkCount);
