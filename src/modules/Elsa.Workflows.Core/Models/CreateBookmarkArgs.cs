namespace Elsa.Workflows.Models;

/// <summary>
/// Provides bookmark creation options.
/// </summary>
public class CreateBookmarkArgs
{
    /// <summary>
    /// A custom ID value to use instead of generating a new one.
    /// </summary>
    public string? BookmarkId { get; set; }

    /// <summary>
    /// An optional stimulus to associate with the bookmark.
    /// </summary>
    public object? Stimulus { get; set; }

    /// <summary>
    /// An optional callback to invoke when the bookmark is triggered.
    /// </summary>
    public ExecuteActivityDelegate? Callback { get; set; }

    /// <summary>
    /// An optional name to associate with the bookmark.
    /// </summary>
    public string? BookmarkName { get; set; }

    /// <summary>
    /// Whether the bookmark should be automatically burned when triggered.
    /// </summary>
    public bool AutoBurn { get; set; } = true;

    /// <summary>
    /// Whether the activity instance ID should be included in the bookmark payload.
    /// </summary>
    public bool IncludeActivityInstanceId { get; set; }

    /// <summary>
    /// Whether the activity being resumed should be automatically completed if CallBack is not specified.
    /// </summary>
    public bool AutoComplete { get; set; } = true;

    /// <summary>
    /// An optional dictionary of metadata to associate with the bookmark.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; set; }
}