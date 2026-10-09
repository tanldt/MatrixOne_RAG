using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TicketshineSupportCopilot.Api.Models;

namespace TicketshineSupportCopilot.Api.Services;

public sealed class OpenAiClient(HttpClient httpClient, AiOptions options) : IAiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<float[]> CreateEmbeddingAsync(string input, CancellationToken cancellationToken)
    {
        options.Validate();
        using var request = CreateRequest(HttpMethod.Post, "https://api.openai.com/v1/embeddings", new
        {
            model = options.EmbeddingModel,
            input
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(value => value.GetSingle())
            .ToArray();
    }

    public async Task<string> CreateChatCompletionAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        options.Validate();
        using var request = CreateRequest(HttpMethod.Post, "https://api.openai.com/v1/chat/completions", new
        {
            model = options.ChatModel,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.2
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, object body)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        return request;
    }
}
