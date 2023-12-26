using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Provides bookmark creation options.
/// </summary>
public class CreateBookmarkArgs
{
    /// <summary>An optional payload to associate with the bookmark.</summary>
    public object? Payload { get; set; }

    /// <summary>An optional callback to invoke when the bookmark is triggered.</summary>
    public ExecuteActivityDelegate? Callback { get; set; }

    /// <summary>An optional name to associate with the bookmark.</summary>
    public string? BookmarkName { get; set; }

    /// <summary>Whether or not the bookmark should be automatically burned when triggered.</summary>
    public bool AutoBurn { get; set; } = true;

    /// <summary>Whether or not the activity instance ID should be included in the bookmark payload.</summary>
    public bool IncludeActivityInstanceId { get; set; }

    /// <summary>
    /// Whether or not the activity being resumed should be automatically completed if CallBack is not specified.
    /// </summary>
    public bool AutoComplete { get; set; } = true;

    /// <summary>
    /// An optional dictionary of metadata to associate with the bookmark.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; set; }
}