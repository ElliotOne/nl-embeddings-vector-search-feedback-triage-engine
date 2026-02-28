using FeedbackTriageVectorSearch.App;
using FeedbackTriageVectorSearch.Data;
using FeedbackTriageVectorSearch.Services;

var config = TriageAppConfig.Load();

Console.WriteLine("Semantic Feedback Triage Engine (.NET, local-first)");
Console.WriteLine($"Embedding model: {config.EmbeddingModel}");
Console.WriteLine($"Window: last {config.WindowDays} days | Cluster threshold: {config.ClusterThreshold:F2}");
Console.WriteLine($"Postgres vector search enabled: {config.EnablePostgresVectorSearch}");
Console.WriteLine();

using var httpClient = new HttpClient { BaseAddress = new Uri(config.OllamaBaseUrl) };
IEmbeddingClient embeddingClient = new OllamaEmbeddingClient(httpClient, config.EmbeddingModel);

var feedback = SampleFeedbackData.Create()
    .Where(x => x.Timestamp >= DateTimeOffset.UtcNow.AddDays(-config.WindowDays))
    .OrderBy(x => x.Timestamp)
    .ThenBy(x => x.Id)
    .ToList();

if (feedback.Count == 0)
{
    Console.WriteLine("No feedback items in the selected time window.");
    return;
}

Console.WriteLine($"Embedding {feedback.Count} feedback items...");
var rows = new List<EmbeddedFeedbackRow>(feedback.Count);

try
{
    foreach (var item in feedback)
    {
        var vector = await embeddingClient.EmbedAsync(item.Text);
        rows.Add(new EmbeddedFeedbackRow(item, vector));
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Embedding request failed: {ex.Message}");
    Console.WriteLine("Start Ollama, then pull the embedding model:");
    Console.WriteLine("  ollama pull nomic-embed-text");
    return;
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Embedding request failed: {ex.Message}");
    return;
}

var index = new FeedbackVectorIndex(rows);
var themeEngine = new FeedbackThemeEngine();
var themes = themeEngine.Cluster(rows, config.ClusterThreshold)
    .Take(config.TopThemes)
    .ToList();

var query = config.QueryText;
var queryVec = await embeddingClient.EmbedAsync(query);
var semanticMatches = index.Search(queryVec, config.TopK, minScore: config.MinSearchScore).ToList();
var fromUtc = DateTimeOffset.UtcNow.AddDays(-config.WindowDays);

// Example relevance labels for demo/testing Recall@K.
var relevantIds = new HashSet<string>(StringComparer.Ordinal)
{
    "F-001", "F-002", "F-007", "F-012", "F-016", "F-020", "F-021", "F-024"
};
var rankedIds = semanticMatches.Select(x => x.Item.Id).ToList();
var recallAt3 = RetrievalMetrics.RecallAtK(relevantIds, rankedIds, 3);
var recallAt5 = RetrievalMetrics.RecallAtK(relevantIds, rankedIds, 5);
var recallAt8 = RetrievalMetrics.RecallAtK(relevantIds, rankedIds, 8);

Console.WriteLine();
Console.WriteLine("Local cosine vector search (in-memory):");
Console.WriteLine($"Semantic query: \"{query}\"");
if (semanticMatches.Count == 0)
{
    Console.WriteLine("No matches above score threshold.");
}
else
{
    foreach (var hit in semanticMatches)
    {
        Console.WriteLine($"{hit.Score:F3} | {hit.Item.Source,-8} | {hit.Item.UserSegment,-10} | {hit.Item.Text}");
    }
}

Console.WriteLine();
Console.WriteLine("Retrieval metrics:");
Console.WriteLine($"Recall@3: {recallAt3:F3}");
Console.WriteLine($"Recall@5: {recallAt5:F3}");
Console.WriteLine($"Recall@8: {recallAt8:F3}");
Console.WriteLine();

Console.WriteLine();
if (config.EnablePostgresVectorSearch && string.IsNullOrWhiteSpace(config.PostgresConnectionString))
{
    Console.WriteLine("Postgres vector search is enabled but TRIAGE_POSTGRES_CONNECTION_STRING is empty.");
}
else if (config.EnablePostgresVectorSearch)
{
    try
    {
        var store = new PostgresFeedbackVectorStore(config.PostgresConnectionString);
        await store.EnsureSchemaAsync();
        await store.UpsertBatchAsync(rows);

        var pgMatches = await store.SearchCosineAsync(queryVec, fromUtc, config.TopK, config.MinSearchScore);

        Console.WriteLine("Postgres pgvector cosine search (Neon/PostgreSQL):");
        Console.WriteLine($"Semantic query: \"{query}\"");
        if (pgMatches.Count == 0)
        {
            Console.WriteLine("No matches above score threshold.");
        }
        else
        {
            foreach (var hit in pgMatches)
            {
                Console.WriteLine($"{hit.Score:F3} | {hit.Item.Source,-8} | {hit.Item.UserSegment,-10} | {hit.Item.Text}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Postgres vector search failed: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("Top themes in selected window:");
Console.WriteLine();

for (var i = 0; i < themes.Count; i++)
{
    var theme = themes[i];
    Console.WriteLine($"Theme #{i + 1}: {theme.ItemCount} reports | Representative similarity: {theme.AverageSimilarity:F3}");
    Console.WriteLine($"Segments: {string.Join(", ", theme.SegmentBreakdown.Select(kv => $"{kv.Key}={kv.Value}"))}");
    Console.WriteLine($"Sources : {string.Join(", ", theme.SourceBreakdown.Select(kv => $"{kv.Key}={kv.Value}"))}");

    foreach (var sample in theme.Examples)
    {
        Console.WriteLine($"  - {sample.Text}");
    }

    Console.WriteLine();
}

Console.WriteLine("Incoming ticket triage:");
var incoming = config.IncomingTicketText;
Console.WriteLine($"\"{incoming}\"");

var incomingVec = await embeddingClient.EmbedAsync(incoming);
var related = index.Search(incomingVec, topK: 5, minScore: config.MinSearchScore).ToList();

if (related.Count == 0)
{
    Console.WriteLine("No related historical tickets above similarity threshold.");
}
else
{
    Console.WriteLine("Most similar previous tickets:");
    foreach (var hit in related)
    {
        Console.WriteLine($"  {hit.Score:F3} | {hit.Item.Id} | {hit.Item.Text}");
    }
}

if (config.EnablePostgresVectorSearch && !string.IsNullOrWhiteSpace(config.PostgresConnectionString))
{
    try
    {
        var store = new PostgresFeedbackVectorStore(config.PostgresConnectionString);
        var pgRelated = await store.SearchCosineAsync(incomingVec, fromUtc, topK: 5, minScore: config.MinSearchScore);

        Console.WriteLine();
        Console.WriteLine("Incoming ticket triage (pgvector cosine):");
        if (pgRelated.Count == 0)
        {
            Console.WriteLine("No related historical tickets above similarity threshold.");
        }
        else
        {
            Console.WriteLine("Most similar previous tickets:");
            foreach (var hit in pgRelated)
            {
                Console.WriteLine($"  {hit.Score:F3} | {hit.Item.Id} | {hit.Item.Text}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Postgres incoming-ticket search failed: {ex.Message}");
    }
}
