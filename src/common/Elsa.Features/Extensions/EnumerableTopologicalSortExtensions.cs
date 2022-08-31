namespace Elsa.Features.Extensions;

public static class EnumerableTopologicalSortExtensions
{
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