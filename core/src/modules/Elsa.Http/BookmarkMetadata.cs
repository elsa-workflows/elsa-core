namespace Elsa.Http;

/// <summary>
/// Provides metadata for HTTP bookmarks.
/// </summary>
public static class BookmarkMetadata
{
    /// <summary>
    /// The metadata key for cross-HTTP boundary activity execution.
    /// </summary>
    public const string HttpCrossBoundaryMetadataKey = "X-HttpCrossBoundary";

    /// <summary>
    /// Provides metadata for cross-HTTP boundary activity execution.
    /// </summary>
    public static readonly IDictionary<string, string> HttpCrossBoundary = new Dictionary<string, string> { [HttpCrossBoundaryMetadataKey] = "true" };
}