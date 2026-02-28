using FeedbackTriageVectorSearch.Domain;

namespace FeedbackTriageVectorSearch.Services;

public sealed record EmbeddedFeedbackRow(FeedbackItem Item, float[] Vector);
