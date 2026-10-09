using TicketshineSupportCopilot.Api.Models;
using TicketshineSupportCopilot.Api.Services;

namespace TicketshineSupportCopilot.Api.Tests;

public sealed class PromptBuilderTests
{
    [Fact]
    public void Build_IncludesCitationMetadata()
    {
        var chunks = new[]
        {
            new RetrievedChunk(10, "ticket-transfer-guide.md", 2, "Missing transfer email", "Check recipient email.", 0.12)
        };

        var (_, userPrompt) = PromptBuilder.Build("What should I check?", chunks);

        Assert.Contains("ticket-transfer-guide.md", userPrompt);
        Assert.Contains("Missing transfer email", userPrompt);
        Assert.Contains("chunk 2", userPrompt);
    }

    [Fact]
    public void Build_InstructsModelToUseOnlyProvidedContext()
    {
        var (systemPrompt, _) = PromptBuilder.Build("What should I check?", []);

        Assert.Contains("Answer only from the provided context", systemPrompt);
    }
}
