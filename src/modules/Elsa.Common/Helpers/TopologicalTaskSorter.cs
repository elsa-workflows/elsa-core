using System.Reflection;

namespace Elsa.Common.Helpers;

/// <summary>
/// Sorts tasks based on their dependencies using topological ordering.
/// </summary>
public static class TopologicalTaskSorter
{
    /// <summary>
    /// Sorts tasks in topological order based on their TaskDependencyAttribute declarations.
    /// </summary>
    /// <param name="tasks">The tasks to sort.</param>
    /// <typeparam name="T">The task type.</typeparam>
    /// <returns>A list of tasks sorted in dependency order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a circular dependency is detected.</exception>
    public static IReadOnlyList<T> Sort<T>(IEnumerable<T> tasks) where T : ITask
    {
        var taskList = tasks.ToList();
        var taskTypes = taskList.Select(t => t.GetType()).ToList();
        var dependencyGraph = BuildDependencyGraph(taskTypes);
        var sortedTypes = TopologicalSort(dependencyGraph);

        // Map sorted types back to original task instances
        var taskMap = taskList.ToDictionary(t => t.GetType(), t => t);
        var result = new List<T>();

        foreach (var type in sortedTypes)
        {
            if (taskMap.TryGetValue(type, out var task))
                result.Add(task);
        }

        return result;
    }

    private static Dictionary<Type, List<Type>> BuildDependencyGraph(IEnumerable<Type> taskTypes)
    {
        var graph = new Dictionary<Type, List<Type>>();

        foreach (var taskType in taskTypes)
        {
            if (!graph.ContainsKey(taskType))
                graph[taskType] = new();

            var dependencies = taskType.GetCustomAttributes<TaskDependencyAttribute>()
                .Select(attr => attr.DependencyTaskType)
                .ToList();

            graph[taskType].AddRange(dependencies);
        }

        return graph;
    }

    private static List<Type> TopologicalSort(Dictionary<Type, List<Type>> graph)
    {
        var sorted = new List<Type>();
        var visited = new HashSet<Type>();
        var visiting = new HashSet<Type>();

        foreach (var node in graph.Keys)
        {
            if (!visited.Contains(node))
                Visit(node, graph, visited, visiting, sorted);
        }

        return sorted;
    }

    private static void Visit(Type node, Dictionary<Type, List<Type>> graph, HashSet<Type> visited, HashSet<Type> visiting, List<Type> sorted)
    {
        if (visiting.Contains(node))
            throw new InvalidOperationException($"Circular dependency detected involving task type: {node.Name}");

        if (visited.Contains(node))
            return;

        visiting.Add(node);

        if (graph.TryGetValue(node, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                Visit(dependency, graph, visited, visiting, sorted);
            }
        }

        visiting.Remove(node);
        visited.Add(node);
        sorted.Add(node);
    }
}
