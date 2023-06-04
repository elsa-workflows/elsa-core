namespace Elsa.Common.Models;

/// <summary>
/// Represents a page of items.
/// </summary>
/// <param name="Items">The items.</param>
/// <param name="TotalCount">The total count.</param>
/// <typeparam name="T">The type of the items.</typeparam>
public record Page<T>(ICollection<T> Items, long TotalCount);

/// <summary>
/// Provides a way to create a new instance of the <see cref="Page{T}"/> class.
/// </summary>
public static class Page
{
    /// <summary>
    /// Creates a new instance of the <see cref="Page{T}"/> class.
    /// </summary>
    /// <param name="items">The items.</param>
    /// <param name="totalCount">The total count.</param>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <returns>A new instance of the <see cref="Page{T}"/> class.</returns>
    public static Page<T> Of<T>(ICollection<T> items, long totalCount) => new(items, totalCount);
}