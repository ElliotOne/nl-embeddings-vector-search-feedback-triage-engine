using FeedbackTriageVectorSearch.Domain;

namespace FeedbackTriageVectorSearch.Data;

public static class SampleFeedbackData
{
    public static List<FeedbackItem> Create()
    {
        var now = DateTimeOffset.UtcNow;

        return new List<FeedbackItem>
        {
            new("F-001", now.AddDays(-1), "Zendesk", "Pro", "I keep getting logged out randomly and have to sign in again."),
            new("F-002", now.AddDays(-1), "AppStore", "Free", "It logs me out every day. Super annoying."),
            new("F-003", now.AddDays(-2), "Intercom", "Enterprise", "CSV export freezes at 90% and never finishes."),
            new("F-004", now.AddDays(-2), "Zendesk", "Pro", "Reports take forever to load. Export is extremely slow."),
            new("F-005", now.AddDays(-3), "NPS", "Enterprise", "Mobile UI is broken on iPhone 14, buttons overlap."),
            new("F-006", now.AddDays(-3), "SalesCall", "Enterprise", "Our finance team waits minutes to download quarterly reports."),
            new("F-007", now.AddDays(-4), "Slack", "Pro", "Session keeps expiring while I am editing dashboards."),
            new("F-008", now.AddDays(-4), "Zendesk", "Free", "Android app dashboard never loads over LTE."),
            new("F-009", now.AddDays(-4), "Intercom", "Pro", "Saved filters disappear after refresh."),
            new("F-010", now.AddDays(-5), "NPS", "Enterprise", "Export job times out on large datasets."),
            new("F-011", now.AddDays(-5), "AppStore", "Free", "Phone layout is a mess after latest update."),
            new("F-012", now.AddDays(-5), "Zendesk", "Enterprise", "We lose draft comments when auth token expires."),
            new("F-013", now.AddDays(-6), "Slack", "Pro", "Could not finish report export. Spinner stuck forever."),
            new("F-014", now.AddDays(-6), "Intercom", "Enterprise", "Need stronger autosave, we lose notes after sign-out."),
            new("F-015", now.AddDays(-6), "Zendesk", "Pro", "Mobile chart labels overlap and become unreadable."),
            new("F-016", now.AddDays(-7), "SalesCall", "Enterprise", "Account executives are blocked by recurring login prompts."),
            new("F-017", now.AddDays(-7), "AppStore", "Free", "Report page hangs forever on my iPhone."),
            new("F-018", now.AddDays(-8), "Zendesk", "Pro", "Team dashboard takes 20 seconds to render widgets."),
            new("F-019", now.AddDays(-2), "NPS", "Enterprise", "Exporting invoices fails near completion."),
            new("F-020", now.AddDays(-3), "Intercom", "Pro", "Random logouts are causing repeated data entry."),
            new("F-021", now.AddDays(-1), "Zendesk", "Enterprise", "Our support reps are kicked out multiple times per shift."),
            new("F-022", now.AddDays(-2), "Slack", "Pro", "Tablet dashboard controls are clipped and untappable."),
            new("F-023", now.AddDays(-1), "SalesCall", "Enterprise", "Leadership report export stalled again during board prep."),
            new("F-024", now.AddDays(-3), "AppStore", "Free", "Cannot keep a session active long enough to submit feedback."),
            new("F-025", now.AddDays(-2), "NPS", "Pro", "Dashboard takes forever on mobile data, maybe image payload is huge.")
        };
    }
}
