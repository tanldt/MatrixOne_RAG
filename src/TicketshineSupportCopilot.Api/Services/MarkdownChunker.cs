using System.Text;
using System.Text.RegularExpressions;
using TicketshineSupportCopilot.Api.Models;

namespace TicketshineSupportCopilot.Api.Services;

public static partial class MarkdownChunker
{
    public static IReadOnlyList<DocumentChunk> Chunk(string documentName, string markdown, int maxCharacters = 1200)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);
        ArgumentException.ThrowIfNullOrWhiteSpace(markdown);

        var chunks = new List<DocumentChunk>();
        var currentHeading = Path.GetFileNameWithoutExtension(documentName);
        var buffer = new StringBuilder();

        foreach (var rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.TrimEnd();
            var heading = ParseHeading(line);

            if (heading is not null)
            {
                FlushBuffer();
                if (!string.IsNullOrWhiteSpace(heading))
                {
                    currentHeading = heading;
                }

                continue;
            }

            if (buffer.Length > 0)
            {
                buffer.AppendLine();
            }

            buffer.Append(line);
        }

        FlushBuffer();
        return chunks;

        void FlushBuffer()
        {
            var content = buffer.ToString().Trim();
            buffer.Clear();

            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            foreach (var part in SplitContent(content, maxCharacters))
            {
                chunks.Add(new DocumentChunk(
                    Id: 0,
                    DocumentId: 0,
                    DocumentName: documentName,
                    ChunkIndex: chunks.Count + 1,
                    Heading: string.IsNullOrWhiteSpace(currentHeading) ? documentName : currentHeading,
                    Content: part));
            }
        }
    }

    private static IEnumerable<string> SplitContent(string content, int maxCharacters)
    {
        if (content.Length <= maxCharacters)
        {
            yield return content;
            yield break;
        }

        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();

        foreach (var word in words)
        {
            if (current.Length > 0 && current.Length + word.Length + 1 > maxCharacters)
            {
                yield return current.ToString();
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(word);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    private static string? ParseHeading(string line)
    {
        var match = HeadingRegex().Match(line);
        return match.Success ? match.Groups["heading"].Value.Trim() : null;
    }

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s*(?<heading>.*?)\s*$")]
    private static partial Regex HeadingRegex();
}
