// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to sort collections by their dependencies, also known as a topological sort.
/// </summary>
public static class EnumerableTopologicalSortExtensions
{
    /// <summary>
    /// Returns a topologically sorted copy of the specified list.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static IEnumerable<T> TSort<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> dependencies, bool throwOnCycle = false)
    {
        var sorted = new List<T>();
        var visited = new HashSet<T>();

        foreach (var item in source)
            Visit(item, visited, sorted, dependencies, throwOnCycle);

        return sorted;
    }

    private static void Visit<T>(T item, ISet<T> visited, ICollection<T> sorted, Func<T, IEnumerable<T>> dependencies, bool throwOnCycle)
    {
        if (!visited.Contains(item))
        {
            visited.Add(item);

            foreach (var dep in dependencies(item))
                Visit(dep, visited, sorted, dependencies, throwOnCycle);

            sorted.Add(item);
        }
        else
        {
            if (throwOnCycle && !sorted.Contains(item))
                throw new Exception("Cyclic dependency found");
        }
    }
}