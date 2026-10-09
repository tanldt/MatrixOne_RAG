namespace TicketshineSupportCopilot.Api.Models;

public sealed record ChatResponse(string Answer, IReadOnlyList<Citation> Citations);
