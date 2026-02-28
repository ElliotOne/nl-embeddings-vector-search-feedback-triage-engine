using System.Globalization;
using FeedbackTriageVectorSearch.Domain;
using Npgsql;

namespace FeedbackTriageVectorSearch.Services;

public sealed class PostgresFeedbackVectorStore
{
    private readonly string _connectionString;

    public PostgresFeedbackVectorStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE EXTENSION IF NOT EXISTS vector;

            CREATE TABLE IF NOT EXISTS feedback_vectors (
                id TEXT PRIMARY KEY,
                occurred_at TIMESTAMPTZ NOT NULL,
                source TEXT NOT NULL,
                user_segment TEXT NOT NULL,
                text_content TEXT NOT NULL,
                embedding vector NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_feedback_vectors_occurred_at
                ON feedback_vectors (occurred_at DESC);
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertBatchAsync(IReadOnlyList<EmbeddedFeedbackRow> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
        {
            return;
        }

        const string sql = """
            INSERT INTO feedback_vectors (id, occurred_at, source, user_segment, text_content, embedding)
            VALUES (@id, @occurredAt, @source, @userSegment, @textContent, CAST(@embedding AS vector))
            ON CONFLICT (id) DO UPDATE SET
                occurred_at = EXCLUDED.occurred_at,
                source = EXCLUDED.source,
                user_segment = EXCLUDED.user_segment,
                text_content = EXCLUDED.text_content,
                embedding = EXCLUDED.embedding;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        foreach (var row in rows)
        {
            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("id", row.Item.Id);
            cmd.Parameters.AddWithValue("occurredAt", row.Item.Timestamp.UtcDateTime);
            cmd.Parameters.AddWithValue("source", row.Item.Source);
            cmd.Parameters.AddWithValue("userSegment", row.Item.UserSegment);
            cmd.Parameters.AddWithValue("textContent", row.Item.Text);
            cmd.Parameters.AddWithValue("embedding", ToVectorLiteral(row.Vector));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(FeedbackItem Item, float Score)>> SearchCosineAsync(
        float[] queryVector,
        DateTimeOffset fromUtc,
        int topK,
        float minScore,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                id,
                occurred_at,
                source,
                user_segment,
                text_content,
                1 - (embedding <=> CAST(@queryEmbedding AS vector)) AS score
            FROM feedback_vectors
            WHERE occurred_at >= @fromUtc
            ORDER BY embedding <=> CAST(@queryEmbedding AS vector)
            LIMIT @topK;
            """;

        var hits = new List<(FeedbackItem Item, float Score)>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("queryEmbedding", ToVectorLiteral(queryVector));
        cmd.Parameters.AddWithValue("fromUtc", fromUtc.UtcDateTime);
        cmd.Parameters.AddWithValue("topK", topK);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var score = Convert.ToSingle(reader.GetValue(5), CultureInfo.InvariantCulture);
            if (score < minScore)
            {
                continue;
            }

            var item = new FeedbackItem(
                id: reader.GetString(0),
                timestamp: new DateTimeOffset(reader.GetDateTime(1), TimeSpan.Zero),
                source: reader.GetString(2),
                userSegment: reader.GetString(3),
                text: reader.GetString(4));

            hits.Add((item, score));
        }

        return hits;
    }

    private static string ToVectorLiteral(float[] vector)
        => $"[{string.Join(",", vector.Select(v => v.ToString("G", CultureInfo.InvariantCulture)))}]";
}
