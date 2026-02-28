using System.Net.Http.Json;

namespace FeedbackTriageVectorSearch.Services;

public interface IEmbeddingClient
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

public sealed class OllamaEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OllamaEmbeddingClient(HttpClient httpClient, string model)
    {
        _httpClient = httpClient;
        _model = model;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var payload = new OllamaEmbeddingsRequest(_model, text);

        using var response = await _httpClient.PostAsJsonAsync("/api/embeddings", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Embedding request failed ({(int)response.StatusCode}): {body}. " +
                "Ensure Ollama is running and the embedding model is pulled.");
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingsResponse>(cancellationToken);
        if (result?.Embedding is null || result.Embedding.Count == 0)
        {
            throw new InvalidOperationException("Ollama embedding response did not contain an embedding vector.");
        }

        return result.Embedding.ToArray();
    }

    private sealed record OllamaEmbeddingsRequest(string Model, string Prompt);

    private sealed class OllamaEmbeddingsResponse
    {
        public List<float> Embedding { get; init; } = new();
    }
}
