using Microsoft.Extensions.Configuration;

namespace TicketshineSupportCopilot.Api.Models;

public sealed record AiOptions(string ApiKey, string EmbeddingModel, string ChatModel)
{
    public static AiOptions FromConfiguration(IConfiguration configuration)
    {
        return new AiOptions(
            ApiKey: configuration["OPENAI_API_KEY"] ?? configuration["OpenAI:ApiKey"] ?? string.Empty,
            EmbeddingModel: configuration["OPENAI_EMBEDDING_MODEL"] ?? configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small",
            ChatModel: configuration["OPENAI_CHAT_MODEL"] ?? configuration["OpenAI:ChatModel"] ?? "gpt-4.1-mini");
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("Missing OPENAI_API_KEY. Set it before running ingestion or chat.");
        }
    }
}
