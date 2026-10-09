using TicketshineSupportCopilot.Api.Models;

namespace TicketshineSupportCopilot.Api.Services;

public sealed class ChatService(IAiClient aiClient, IMatrixOneRepository repository)
{
    public async Task<ChatResponse> AnswerAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            throw new ArgumentException("Question is required.", nameof(request));
        }

        var questionEmbedding = await aiClient.CreateEmbeddingAsync(request.Question, cancellationToken);
        var chunks = await repository.SearchSimilarChunksAsync(questionEmbedding, limit: 5, cancellationToken);

        if (chunks.Count == 0)
        {
            return new ChatResponse("No indexed support documents were found. Run ingestion first, then ask again.", []);
        }

        var (systemPrompt, userPrompt) = PromptBuilder.Build(request.Question, chunks);
        var answer = await aiClient.CreateChatCompletionAsync(systemPrompt, userPrompt, cancellationToken);
        var citations = chunks
            .Select(chunk => new Citation(chunk.DocumentName, chunk.Heading, chunk.ChunkIndex))
            .ToArray();

        return new ChatResponse(answer, citations);
    }
}
