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
    public static Diff<T> For<T>(ICollection<T> firstSet, ICollection<T> secondSet)
    {
        var removed = firstSet.Except(secondSet).ToList();
        var added = secondSet.Except(firstSet).ToList();
        var unchanged = firstSet.Intersect(secondSet).ToList();

        return new Diff<T>(added, removed, unchanged);
    }
}