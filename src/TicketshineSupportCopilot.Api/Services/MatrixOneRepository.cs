using System.Globalization;
using MySqlConnector;
using TicketshineSupportCopilot.Api.Models;

namespace TicketshineSupportCopilot.Api.Services;

public sealed class MatrixOneRepository(MatrixOneOptions options) : IMatrixOneRepository
{
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(options.CreateConnectionString(includeDatabase: false));
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = SchemaSql.Replace("ticketshine_rag", options.Database, StringComparison.Ordinal);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ResetDemoDataAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(options.CreateConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM document_chunks; DELETE FROM documents;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IndexedDocument> InsertDocumentWithChunksAsync(
        string name,
        string sourcePath,
        IReadOnlyList<(DocumentChunk Chunk, float[] Embedding)> chunks,
        CancellationToken cancellationToken)
    {
        var documentId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + Math.Abs(name.GetHashCode());

        await using var connection = new MySqlConnection(options.CreateConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var insertDocument = connection.CreateCommand();
        insertDocument.CommandText = "INSERT INTO documents (id, name, source_path) VALUES (@id, @name, @sourcePath);";
        insertDocument.Parameters.AddWithValue("@id", documentId);
        insertDocument.Parameters.AddWithValue("@name", name);
        insertDocument.Parameters.AddWithValue("@sourcePath", sourcePath);
        await insertDocument.ExecuteNonQueryAsync(cancellationToken);

        for (var i = 0; i < chunks.Count; i++)
        {
            var (chunk, embedding) = chunks[i];
            await using var insertChunk = connection.CreateCommand();
            insertChunk.CommandText = """
                INSERT INTO document_chunks
                    (id, document_id, document_name, chunk_index, heading, content, embedding)
                VALUES
                    (@id, @documentId, @documentName, @chunkIndex, @heading, @content, @embedding);
                """;
            insertChunk.Parameters.AddWithValue("@id", documentId * 1000 + i + 1);
            insertChunk.Parameters.AddWithValue("@documentId", documentId);
            insertChunk.Parameters.AddWithValue("@documentName", name);
            insertChunk.Parameters.AddWithValue("@chunkIndex", chunk.ChunkIndex);
            insertChunk.Parameters.AddWithValue("@heading", (object?)chunk.Heading ?? DBNull.Value);
            insertChunk.Parameters.AddWithValue("@content", chunk.Content);
            insertChunk.Parameters.AddWithValue("@embedding", FormatVectorLiteral(embedding));
            await insertChunk.ExecuteNonQueryAsync(cancellationToken);
        }

        return new IndexedDocument(documentId, name, sourcePath, chunks.Count);
    }

    public async Task<IReadOnlyList<IndexedDocument>> ListDocumentsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(options.CreateConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT d.id, d.name, d.source_path, COUNT(c.id) AS chunk_count
            FROM documents d
            LEFT JOIN document_chunks c ON c.document_id = d.id
            GROUP BY d.id, d.name, d.source_path
            ORDER BY d.name;
            """;

        var documents = new List<IndexedDocument>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            documents.Add(new IndexedDocument(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture)));
        }

        return documents;
    }

    public async Task<IReadOnlyList<RetrievedChunk>> SearchSimilarChunksAsync(float[] embedding, int limit, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(options.CreateConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = BuildSearchCommandText();
        command.Parameters.AddWithValue("@embedding", FormatVectorLiteral(embedding));
        command.Parameters.AddWithValue("@limit", limit);

        var chunks = new List<RetrievedChunk>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            chunks.Add(new RetrievedChunk(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4),
                reader.GetDouble(5)));
        }

        return chunks;
    }

    public static string BuildSearchCommandText()
    {
        return """
            SELECT
                id,
                document_name,
                chunk_index,
                heading,
                content,
                l2_distance(embedding, @embedding) AS distance
            FROM document_chunks
            ORDER BY distance ASC
            LIMIT @limit;
            """;
    }

    public static string FormatVectorLiteral(float[] embedding)
    {
        return "[" + string.Join(",", embedding.Select(value => value.ToString("0.#######", CultureInfo.InvariantCulture))) + "]";
    }

    private const string SchemaSql = """
        CREATE DATABASE IF NOT EXISTS ticketshine_rag;
        USE ticketshine_rag;

        CREATE TABLE IF NOT EXISTS documents (
            id BIGINT PRIMARY KEY,
            name VARCHAR(255) NOT NULL,
            source_path VARCHAR(500) NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS document_chunks (
            id BIGINT PRIMARY KEY,
            document_id BIGINT NOT NULL,
            document_name VARCHAR(255) NOT NULL,
            chunk_index INT NOT NULL,
            heading VARCHAR(255),
            content TEXT NOT NULL,
            embedding VECF32(1536),
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        """;
}
