using Microsoft.Extensions.Configuration;
using TicketshineSupportCopilot.Api.Models;

namespace TicketshineSupportCopilot.Api.Tests;

public sealed class OpenAiClientConfigurationTests
{
    [Fact]
    public void AiOptions_MissingApiKey_IsInvalid()
    {
        var options = AiOptions.FromConfiguration(new ConfigurationBuilder().Build());

        var error = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains("OPENAI_API_KEY", error.Message);
    }

    [Fact]
    public void AiOptions_DefaultModels_AreSpecDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OPENAI_API_KEY"] = "test-key"
            })
            .Build();

        var options = AiOptions.FromConfiguration(configuration);

        Assert.Equal("text-embedding-3-small", options.EmbeddingModel);
        Assert.Equal("gpt-4.1-mini", options.ChatModel);
    }
}
