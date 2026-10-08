namespace Elsa.Workflows.Models;

/// <summary>
/// Represents information about a bookmark.
/// </summary>
public class BookmarkInfo
{
    /// <summary>
    /// Gets or sets the ID of the bookmark.
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Gets or sets the name of the bookmark.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the hash of the bookmark.
    /// </summary>
    public string Hash { get; set; } = default!;
}