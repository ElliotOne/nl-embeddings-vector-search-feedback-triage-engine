namespace FeedbackTriageVectorSearch.Services;

public static class RetrievalMetrics
{
    public static double RecallAtK(
        IReadOnlyCollection<string> relevantIds,
        IReadOnlyList<string> rankedIds,
        int k)
    {
        var top = rankedIds.Take(k).ToHashSet(StringComparer.Ordinal);
        var hits = relevantIds.Count(id => top.Contains(id));
        return relevantIds.Count == 0 ? 0 : (double)hits / relevantIds.Count;
    }
}
