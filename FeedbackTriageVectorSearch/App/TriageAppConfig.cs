using Microsoft.Extensions.Configuration;

namespace FeedbackTriageVectorSearch.App;

public sealed class TriageAppConfig
{
    public bool EnablePostgresVectorSearch { get; init; } = false;
    public string PostgresConnectionString { get; init; } = string.Empty;
    public string OllamaBaseUrl { get; init; } = "http://localhost:11434";
    public string EmbeddingModel { get; init; } = "nomic-embed-text";
    public int WindowDays { get; init; } = 7;
    public int TopK { get; init; } = 8;
    public int TopThemes { get; init; } = 5;
    public float ClusterThreshold { get; init; } = 0.75f;
    public float MinSearchScore { get; init; } = 0.50f;
    public string QueryText { get; init; } = "users are being signed out all the time";
    public string IncomingTicketText { get; init; } = "Every few hours our team is forced to login again and unsaved edits are lost.";

    public static TriageAppConfig Load()
    {
        var envName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: false)
            .Build();

        var fromJson = configuration.GetSection("Triage").Get<TriageAppConfig>() ?? new TriageAppConfig();

        return new TriageAppConfig
        {
            EnablePostgresVectorSearch = GetBool("TRIAGE_USE_POSTGRES_VECTOR_SEARCH", fromJson.EnablePostgresVectorSearch),
            PostgresConnectionString = GetString("TRIAGE_POSTGRES_CONNECTION_STRING", fromJson.PostgresConnectionString),
            OllamaBaseUrl = GetString("TRIAGE_OLLAMA_BASE_URL", fromJson.OllamaBaseUrl),
            EmbeddingModel = GetString("TRIAGE_EMBEDDING_MODEL", fromJson.EmbeddingModel),
            WindowDays = GetInt("TRIAGE_WINDOW_DAYS", fromJson.WindowDays, min: 1, max: 365),
            TopK = GetInt("TRIAGE_TOP_K", fromJson.TopK, min: 1, max: 50),
            TopThemes = GetInt("TRIAGE_TOP_THEMES", fromJson.TopThemes, min: 1, max: 20),
            ClusterThreshold = GetFloat("TRIAGE_CLUSTER_THRESHOLD", fromJson.ClusterThreshold, min: 0.5f, max: 0.99f),
            MinSearchScore = GetFloat("TRIAGE_MIN_SEARCH_SCORE", fromJson.MinSearchScore, min: 0.0f, max: 0.99f),
            QueryText = GetString("TRIAGE_QUERY", fromJson.QueryText),
            IncomingTicketText = GetString("TRIAGE_INCOMING", fromJson.IncomingTicketText)
        };
    }

    private static string GetString(string name, string fallback)
        => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))
            ? fallback
            : Environment.GetEnvironmentVariable(name)!.Trim();

    private static int GetInt(string name, int fallback, int min, int max)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (!int.TryParse(raw, out var parsed))
        {
            return fallback;
        }

        return Math.Clamp(parsed, min, max);
    }

    private static float GetFloat(string name, float fallback, float min, float max)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (!float.TryParse(raw, out var parsed))
        {
            return fallback;
        }

        return Math.Clamp(parsed, min, max);
    }

    private static bool GetBool(string name, bool fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (!bool.TryParse(raw, out var parsed))
        {
            return fallback;
        }

        return parsed;
    }
}
