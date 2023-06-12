namespace Elsa.Common.Models;

/// <summary>
/// Represents pagination arguments.
/// </summary>
/// <param name="Page">The zero-based page number.</param>
/// <param name="PageSize">The number of items per page.</param>
public record PageArgs(int? Page, int? PageSize)
{
    /// <summary>
    /// Gets the offset of the page.
    /// </summary>
    public int? Offset => Page * PageSize;

    /// <summary>
    /// Gets the limit of the page.
    /// </summary>
    public int? Limit => PageSize;

    /// <summary>
    /// Returns pagination arguments for the next page.
    /// </summary>
    /// <returns>The arguments for the next page.</returns>
    public PageArgs Next() => this with { Page = Page + 1 };
}