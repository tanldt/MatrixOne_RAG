using System.Text;
using TicketshineSupportCopilot.Api.Models;

namespace TicketshineSupportCopilot.Api.Services;

public static class PromptBuilder
{
    public static (string SystemPrompt, string UserPrompt) Build(string question, IReadOnlyList<RetrievedChunk> chunks)
    {
        const string systemPrompt = """
            Answer only from the provided context.
            If the answer is not available in the context, say what is missing.
            Cite sources by document name and chunk number.
            Keep support replies practical and agent-ready.
            """;

        var userPrompt = new StringBuilder();
        userPrompt.AppendLine("Context chunks:");
        foreach (var chunk in chunks)
        {
            userPrompt.AppendLine($"[chunk {chunk.ChunkIndex}] document: {chunk.DocumentName}");
            if (!string.IsNullOrWhiteSpace(chunk.Heading))
            {
                userPrompt.AppendLine($"heading: {chunk.Heading}");
            }

            userPrompt.AppendLine(chunk.Content);
            userPrompt.AppendLine();
        }

        userPrompt.AppendLine($"Question: {question}");
        return (systemPrompt, userPrompt.ToString());
    }
}
