namespace FeedbackTriageVectorSearch.Domain;

public sealed class FeedbackItem
{
    public string Id { get; }
    public DateTimeOffset Timestamp { get; }
    public string Source { get; }
    public string UserSegment { get; }
    public string Text { get; }

    public FeedbackItem(
        string id,
        DateTimeOffset timestamp,
        string source,
        string userSegment,
        string text)
    {
        Id = id;
        Timestamp = timestamp;
        Source = source;
        UserSegment = userSegment;
        Text = text;
    }
}
