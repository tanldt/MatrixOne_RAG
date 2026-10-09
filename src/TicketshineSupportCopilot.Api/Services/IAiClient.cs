namespace TicketshineSupportCopilot.Api.Services;

public interface IAiClient
{
    Task<float[]> CreateEmbeddingAsync(string input, CancellationToken cancellationToken);

    Task<string> CreateChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}
