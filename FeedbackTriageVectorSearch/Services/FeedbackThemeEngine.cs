using FeedbackTriageVectorSearch.Domain;

namespace FeedbackTriageVectorSearch.Services;

public sealed class FeedbackTheme
{
    public IReadOnlyList<FeedbackItem> Items { get; init; } = Array.Empty<FeedbackItem>();
    public IReadOnlyList<FeedbackItem> Examples { get; init; } = Array.Empty<FeedbackItem>();
    public int ItemCount { get; init; }
    public float AverageSimilarity { get; init; }
    public IReadOnlyDictionary<string, int> SourceBreakdown { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> SegmentBreakdown { get; init; } = new Dictionary<string, int>();
}

public sealed class FeedbackThemeEngine
{
    public IReadOnlyList<FeedbackTheme> Cluster(IReadOnlyList<EmbeddedFeedbackRow> rows, float threshold)
    {
        var clusters = new List<ClusterState>();

        foreach (var row in rows)
        {
            ClusterState? bestCluster = null;
            var bestScore = float.MinValue;

            foreach (var cluster in clusters)
            {
                var score = VectorMath.CosineSimilarity(cluster.Centroid, row.Vector);
                if (score >= threshold && score > bestScore)
                {
                    bestScore = score;
                    bestCluster = cluster;
                }
            }

            if (bestCluster is null)
            {
                clusters.Add(new ClusterState(row));
            }
            else
            {
                bestCluster.Add(row, bestScore);
            }
        }

        return clusters
            .Select(ToTheme)
            .OrderByDescending(theme => theme.ItemCount)
            .ThenByDescending(theme => theme.AverageSimilarity)
            .ToList();
    }

    private static FeedbackTheme ToTheme(ClusterState state)
    {
        var orderedItems = state.Members
            .OrderByDescending(x => x.Item.Timestamp)
            .ThenBy(x => x.Item.Id, StringComparer.Ordinal)
            .Select(x => x.Item)
            .ToList();

        var sourceBreakdown = orderedItems
            .GroupBy(x => x.Source)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var segmentBreakdown = orderedItems
            .GroupBy(x => x.UserSegment)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var examples = state.Members
            .OrderByDescending(x => VectorMath.CosineSimilarity(state.Centroid, x.Vector))
            .ThenBy(x => x.Item.Id, StringComparer.Ordinal)
            .Take(3)
            .Select(x => x.Item)
            .ToList();

        return new FeedbackTheme
        {
            Items = orderedItems,
            Examples = examples,
            ItemCount = orderedItems.Count,
            AverageSimilarity = state.AverageMemberSimilarity,
            SourceBreakdown = sourceBreakdown,
            SegmentBreakdown = segmentBreakdown
        };
    }

    private sealed class ClusterState
    {
        private float _similarityAccumulator;

        public ClusterState(EmbeddedFeedbackRow seed)
        {
            Members = new List<EmbeddedFeedbackRow> { seed };
            Centroid = VectorMath.Clone(seed.Vector);
            _similarityAccumulator = 1.0f;
        }

        public List<EmbeddedFeedbackRow> Members { get; }
        public float[] Centroid { get; }

        public float AverageMemberSimilarity => _similarityAccumulator / Members.Count;

        public void Add(EmbeddedFeedbackRow row, float similarityToCentroid)
        {
            var previousCount = Members.Count;
            Members.Add(row);
            _similarityAccumulator += similarityToCentroid;

            // Incremental mean: new = ((old * n) + x) / (n + 1)
            for (var i = 0; i < Centroid.Length; i++)
            {
                Centroid[i] = ((Centroid[i] * previousCount) + row.Vector[i]) / Members.Count;
            }
        }
    }
}
