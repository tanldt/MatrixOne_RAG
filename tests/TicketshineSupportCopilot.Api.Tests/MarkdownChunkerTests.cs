using TicketshineSupportCopilot.Api.Services;

namespace TicketshineSupportCopilot.Api.Tests;

public sealed class MarkdownChunkerTests
{
    [Fact]
    public void Chunk_KeepsHeadingMetadata()
    {
        const string markdown = """
            # Transfer Guide

            ## Missing transfer email

            Check whether the recipient email is correct and whether the provider has generated the transfer.
            """;

        var chunks = MarkdownChunker.Chunk("ticket-transfer-guide.md", markdown);

        var chunk = Assert.Single(chunks);
        Assert.Equal("Missing transfer email", chunk.Heading);
        Assert.Contains("recipient email", chunk.Content);
    }

    [Fact]
    public void Chunk_SplitsLongSectionByMaxCharacters()
    {
        var markdown = "## Long Section\n\n" + string.Join(" ", Enumerable.Repeat("delayed order investigation", 80));

        var chunks = MarkdownChunker.Chunk("delayed-orders-playbook.md", markdown, maxCharacters: 1200);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.True(chunk.Content.Length <= 1200));
    }

    [Fact]
    public void Chunk_RepeatedOrEmptyHeadingsStillProducesUsefulMetadata()
    {
        const string markdown = """
            ## Refunds

            Refunds need documented delivery failure.

            ##

            Empty headings should not erase useful metadata.

            ## Refunds

            Repeated heading keeps the same useful label.
            """;

        var chunks = MarkdownChunker.Chunk("refund-policy.md", markdown, maxCharacters: 80);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk.Heading)));
        Assert.Contains(chunks, chunk => chunk.Heading == "Refunds");
    }
}
