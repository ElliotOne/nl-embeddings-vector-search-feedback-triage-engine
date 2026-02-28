# nl-embeddings-vector-search-feedback-triage-engine

An educational **local-first semantic customer feedback triage engine** built in C#.

This project turns messy feedback language into deterministic product signal using:

- local embeddings with Ollama
- in-memory cosine vector search
- PostgreSQL/Neon `pgvector` cosine search
- lightweight clustering into weekly issue themes
- source and segment impact rollups

No agents. No RAG. No generative summarization required.

## Overview

This repository models a common SaaS workflow:

1. Embed support and product feedback as vectors.
2. Retrieve semantically similar feedback for new incidents.
3. Cluster weekly feedback into high-volume themes.
4. Output top issues with example tickets and impact slices.

The system is designed to be simple, inspectable, and production-shaped: you get both local search and persistent pgvector search in the same flow.

## What This Project Demonstrates

- A strict domain model (`FeedbackItem`) with metadata preserved outside embeddings
- Ollama embedding integration via `/api/embeddings`
- Deterministic cosine-similarity retrieval (`FeedbackVectorIndex`)
- Persistent cosine vector search in PostgreSQL (`PostgresFeedbackVectorStore`) using pgvector operator `<=>`
- Threshold-driven, centroid-based online clustering (`FeedbackThemeEngine`)
- Time-window filtering and operational knobs via environment variables
- Practical triage output for support, product, and engineering

## Prerequisites

- .NET 10 SDK or later
- Ollama running locally
- PostgreSQL with `pgvector` extension enabled (Neon works)
- Embedding model pulled locally:

```bash
ollama pull nomic-embed-text
```

## Quick Start

From the project root:

```bash
dotnet run --project FeedbackTriageVectorSearch
```

The app will:

- embed a realistic sample feedback dataset
- run semantic retrieval in-memory with cosine similarity
- optionally ingest vectors into PostgreSQL and run pgvector cosine search
- print top weekly themes with segment/source distribution
- simulate triage of a new incoming ticket against prior issues

## Configuration

Primary configuration is in:

- `FeedbackTriageVectorSearch/appsettings.json` under the `Triage` section

Environment variables are optional overrides:

- `TRIAGE_OLLAMA_BASE_URL` (default: `http://localhost:11434`)
- `TRIAGE_EMBEDDING_MODEL` (default: `nomic-embed-text`)
- `TRIAGE_USE_POSTGRES_VECTOR_SEARCH` (default: `false`)
- `TRIAGE_POSTGRES_CONNECTION_STRING` (default: empty)
- `TRIAGE_WINDOW_DAYS` (default: `7`)
- `TRIAGE_CLUSTER_THRESHOLD` (default: `0.75`)
- `TRIAGE_TOP_K` (default: `8`)
- `TRIAGE_TOP_THEMES` (default: `5`)
- `TRIAGE_MIN_SEARCH_SCORE` (default: `0.50`)
- `TRIAGE_QUERY` (semantic retrieval query)
- `TRIAGE_INCOMING` (incoming ticket text for known-issue lookup)

Example `appsettings.json`:

```json
{
  "Triage": {
    "EnablePostgresVectorSearch": false,
    "PostgresConnectionString": "",
    "OllamaBaseUrl": "http://localhost:11434",
    "EmbeddingModel": "nomic-embed-text",
    "WindowDays": 7,
    "TopK": 8,
    "TopThemes": 5,
    "ClusterThreshold": 0.75,
    "MinSearchScore": 0.5,
    "QueryText": "users are being signed out all the time",
    "IncomingTicketText": "Every few hours our team is forced to login again and unsaved edits are lost."
  }
}
```

Example:

```bash
set TRIAGE_USE_POSTGRES_VECTOR_SEARCH=true
set TRIAGE_POSTGRES_CONNECTION_STRING=Host=ep-xxx.us-east-2.aws.neon.tech;Database=neondb;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true
set TRIAGE_WINDOW_DAYS=14
set TRIAGE_CLUSTER_THRESHOLD=0.82
set TRIAGE_QUERY=users keep getting signed out during edits


dotnet run --project FeedbackTriageVectorSearch
```

## Project Structure

```text
.
+-- FeedbackTriageVectorSearch.slnx
+-- FeedbackTriageVectorSearch/
|   +-- FeedbackTriageVectorSearch.csproj
|   +-- Program.cs
|   +-- App/
|   |   +-- TriageAppConfig.cs
|   +-- Data/
|   |   +-- SampleFeedbackData.cs
|   +-- Domain/
|   |   +-- FeedbackItem.cs
|   +-- Services/
|   |   +-- EmbeddedFeedbackRow.cs
|   |   +-- IEmbeddingClient.cs
|   |   +-- VectorMath.cs
|   |   +-- FeedbackVectorIndex.cs
|   |   +-- FeedbackThemeEngine.cs
|   |   +-- PostgresFeedbackVectorStore.cs
+-- LICENSE
+-- README.md
```

## Engineering Notes

- Embeddings provide deterministic semantic representation.
- Vector search is nearest-neighbor ranking, not reasoning.
- Cosine similarity is used in both paths:
  - C# in-memory cosine math (`VectorMath.CosineSimilarity`)
  - PostgreSQL pgvector cosine distance (`embedding <=> query`), converted to similarity as `1 - distance`
- Similarity thresholds are operational controls:
  - above threshold => same theme
  - below threshold => new issue cluster

## Suggested Extensions

- Add an HNSW index strategy for higher-scale pgvector retrieval
- Add hybrid lexical+vector scoring for precision-sensitive queues
- Maintain durable theme prototypes and weekly deltas
- Add labeled evaluation (`Recall@K`, cluster purity)
- Add a queue processor for continuous ingestion

## License

See the [LICENSE](LICENSE) file.

## Contributing

Contributions are welcome. Useful extensions include:

- Better clustering strategies and threshold calibration tooling
- Connectors for Zendesk/Intercom/App Store ingestion
- Evaluation harnesses for retrieval and theme quality
- Observability metrics for embedding and retrieval latency
