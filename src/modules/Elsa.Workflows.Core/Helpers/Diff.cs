namespace Elsa.Workflows.Core.Helpers;

/// <summary>
/// A utility that compares two collections and returns a set of added, removed and unchanged items.
/// </summary>
public class Diff<T>
{
    internal Diff(ICollection<T> added, ICollection<T> removed, ICollection<T> unchanged)
    {
        Added = added;
        Removed = removed;
        Unchanged = unchanged;
    }

    /// <summary>
    /// The added items.
    /// </summary>
    public ICollection<T> Added { get; }
    
    /// <summary>
    /// The removed items.
    /// </summary>
    public ICollection<T> Removed { get; }
    
    /// <summary>
    /// The unchanged items.
    /// </summary>
    public ICollection<T> Unchanged { get; }
}

/// <summary>
/// A factory class to construct new <see cref="Diff{T}"/> objects.
/// </summary>
public static class Diff
{
    /// <summary>
    /// Returns an empty diff.
    /// </summary>
    public static Diff<T> Empty<T>() => new(Array.Empty<T>(), Array.Empty<T>(), Array.Empty<T>());
    
    /// <summary>
    /// Create a diff between two sets.
    /// </summary>
    public static Diff<T> For<T>(ICollection<T> firstSet, ICollection<T> secondSet, IEqualityComparer<T>? comparer = default)
    {
        var removed = firstSet.Except(secondSet, comparer).ToList();
        var added = secondSet.Except(firstSet, comparer).ToList();
        var unchanged = firstSet.Intersect(secondSet, comparer).ToList();

        return new Diff<T>(added, removed, unchanged);
    }
}