namespace Elsa.Workflows.Models;

/// Provides bookmark creation options.
public class CreateBookmarkArgs
{
    /// A custom ID value to use instead of generating a new one.
    public string? BookmarkId { get; set; }

    /// An optional stimulus to associate with the bookmark.
    public object? Stimulus { get; set; }

    /// An optional callback to invoke when the bookmark is triggered.
    public ExecuteActivityDelegate? Callback { get; set; }

    /// An optional name to associate with the bookmark.
    public string? BookmarkName { get; set; }

    /// Whether the bookmark should be automatically burned when triggered.
    public bool AutoBurn { get; set; } = true;

    /// Whether the activity instance ID should be included in the bookmark payload.
    public bool IncludeActivityInstanceId { get; set; }

    /// Whether the activity being resumed should be automatically completed if CallBack is not specified.
    public bool AutoComplete { get; set; } = true;

    /// An optional dictionary of metadata to associate with the bookmark.
    public IDictionary<string, string>? Metadata { get; set; }
}