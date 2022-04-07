namespace Elsa.Helpers;

/// <summary>
/// A simple utility that compares two collections and returns a set of added, removed and unchanged items.
/// </summary>
public class Diff<T>
{
    internal Diff(ICollection<T> added, ICollection<T> removed, ICollection<T> unchanged)
    {
        Added = added;
        Removed = removed;
        Unchanged = unchanged;
    }

    public ICollection<T> Added { get; }
    public ICollection<T> Removed { get; }
    public ICollection<T> Unchanged { get; }
}

public static class Diff
{
    public static Diff<T> For<T>(ICollection<T> firstSet, ICollection<T> secondSet, IEqualityComparer<T>? comparer = default)
    {
        var removed = firstSet.Except(secondSet, comparer).ToList();
        var added = secondSet.Except(firstSet, comparer).ToList();
        var unchanged = firstSet.Intersect(secondSet, comparer).ToList();

        return new Diff<T>(added, removed, unchanged);
    }
}