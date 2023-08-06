using Elsa.Common.Models;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extension methods for <see cref="IEnumerable{T}"/>.
/// </summary>
[PublicAPI]
public static class EnumerableExtensions
{
    /// <summary>
    /// Paginates the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to paginate.</param>
    /// <param name="projection">The projection to apply to the enumerable.</param>
    /// <param name="pageArgs">The pagination arguments.</param>
    /// <typeparam name="T">The type of the enumerable.</typeparam>
    /// <typeparam name="TTarget">The type of the projection.</typeparam>
    /// <returns>A page of the projected enumerable.</returns>
    public static Page<TTarget> Paginate<T, TTarget>(this IEnumerable<T> enumerable, Func<T, TTarget> projection, PageArgs? pageArgs = default)
    {
        var items = enumerable.ToList();
        var count = items.Count;

        if (pageArgs?.Offset != null) items = items.Skip(pageArgs.Offset.Value).ToList();
        if (pageArgs?.Limit != null) items = items.Take(pageArgs.Limit.Value).ToList();

        var results = items.Select(projection).ToList();
        return Page.Of(results, count);
    }

    /// <summary>
    /// Paginates the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to paginate.</param>
    /// <param name="pageArgs">The pagination arguments.</param>
    /// <typeparam name="T">The type of the enumerable.</typeparam>
    /// <returns>A page of the enumerable.</returns>
    public static Page<T> Paginate<T>(this IEnumerable<T> enumerable, PageArgs? pageArgs = default)
    {
        var items = enumerable.ToList();
        var count = items.Count;

        if (pageArgs?.Offset != null) items = items.Skip(pageArgs.Offset.Value).ToList();
        if (pageArgs?.Limit != null) items = items.Take(pageArgs.Limit.Value).ToList();

        var results = items.ToList();
        return Page.Of(results, count);
    }

    /// <summary>
    /// Returns a value indicating whether two lists are equal.
    /// </summary>
    /// <param name="list1">The first list.</param>
    /// <param name="list2">The second list.</param>
    /// <typeparam name="T">The type of the items in the lists.</typeparam>
    /// <returns>True if the lists are equal, otherwise false.</returns>
    public static bool IsEqualTo<T>(this IEnumerable<T> list1, IEnumerable<T> list2) => list1.OrderBy(g => g).SequenceEqual(list2.OrderBy(g => g));
}