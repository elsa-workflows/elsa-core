using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Provides bookmark creation options.
/// </summary>
/// <param name="Payload">An optional payload to associate with the bookmark.</param>
/// <param name="Callback">An optional callback to invoke when the bookmark is triggered.</param>
/// <param name="BookmarkName">An optional name to associate with the bookmark.</param>
/// <param name="AutoBurn">Whether or not the bookmark should be automatically burned when triggered.</param>
/// <param name="IncludeActivityInstanceId">Whether or not the activity instance ID should be included in the bookmark payload.</param>
public record CreateBookmarkArgs(
    object? Payload = default, 
    ExecuteActivityDelegate? Callback = default, 
    string? BookmarkName = default, 
    bool AutoBurn = true, 
    bool IncludeActivityInstanceId = true,
    IDictionary<string, string>? Metadata = default);