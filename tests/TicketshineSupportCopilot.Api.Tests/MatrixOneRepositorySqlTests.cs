using TicketshineSupportCopilot.Api.Services;

namespace TicketshineSupportCopilot.Api.Tests;

public sealed class MatrixOneRepositorySqlTests
{
    [Fact]
    public void SearchSimilarChunks_UsesParameterizedLimitAndDoesNotUseQuestionText()
    {
        var command = MatrixOneRepository.BuildSearchCommandText();

        Assert.Contains("@embedding", command);
        Assert.Contains("@limit", command);
        Assert.DoesNotContain("@question", command);
        Assert.DoesNotContain("Question", command);
    }

    [Fact]
    public void VectorLiteral_FormatsInvariantCulture()
    {
        var literal = MatrixOneRepository.FormatVectorLiteral([0.1f, -0.2f]);

        Assert.Equal("[0.1,-0.2]", literal);
    }
}
