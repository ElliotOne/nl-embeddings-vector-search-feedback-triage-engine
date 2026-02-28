using FeedbackTriageVectorSearch.Domain;

namespace FeedbackTriageVectorSearch.Services;

public sealed class FeedbackVectorIndex
{
    private readonly IReadOnlyList<EmbeddedFeedbackRow> _rows;

    public FeedbackVectorIndex(IReadOnlyList<EmbeddedFeedbackRow> rows)
    {
        _rows = rows;
    }

    public IEnumerable<(FeedbackItem Item, float Score)> Search(float[] queryVector, int topK, float minScore = 0f)
    {
        return _rows
            .Select(row => (row.Item, Score: VectorMath.CosineSimilarity(row.Vector, queryVector)))
            .Where(hit => hit.Score >= minScore)
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Item.Id, StringComparer.Ordinal)
            .Take(topK);
    }
}
