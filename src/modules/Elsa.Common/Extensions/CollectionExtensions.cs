// ReSharper disable once CheckNamespace
using System.Diagnostics.CodeAnalysis;

namespace Elsa.Extensions;

class TypeComparer<T> : IEqualityComparer<T>
{
    public bool Equals(T x, T y)
    {
        return (x?.GetType() == y?.GetType());
    }

    public int GetHashCode([DisallowNull] T obj)
    {
        return obj.GetType().GetHashCode();
    }
}

/// <summary>
/// Provides extension methods for collections.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Adds a range of items to a collection.
    /// </summary>
    /// <param name="target">The target collection.</param>
    /// <param name="source">The source collection.</param>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
    {
        foreach (var item in source) target.Add(item);
    }

    /// <summary>
    /// Adds a range of items to a collection.
    /// </summary>
    /// <param name="target">The target collection.</param>
    /// <param name="source">The source collection.</param>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public static void AddRange<T>(this ICollection<T> target, params T[] source) => AddRange(target, source.AsEnumerable());

    /// <summary>
    /// Removes all items from a collection that match the specified predicate.
    /// </summary>
    /// <param name="collection">The collection.</param>
    /// <param name="predicate">The predicate.</param>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public static void RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate)
    {
        var itemsToRemove = collection.Where(predicate).ToList();
        foreach (var item in itemsToRemove) collection.Remove(item);
    }

    /// <summary>
    /// Add a derived type to the collection if it does not already exist.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="type">The unique type</param>
    /// <returns></returns>
    public static bool AddUniqueType<T>(this ICollection<T> collection, T type)
    {
        if (!collection.Contains(type, new TypeComparer<T>()))
        {
            collection.Add(type);
            return true;
        }
        return false;
    }
}