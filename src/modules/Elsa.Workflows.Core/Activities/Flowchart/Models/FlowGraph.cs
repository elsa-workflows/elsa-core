using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Extensions;

namespace Elsa.Workflows.Activities.Flowchart.Models;

/// <summary>
/// Represents a directed graph structure for managing workflow connections.
/// Caches forward and backward connections to optimize graph traversal.
/// </summary>
public class FlowGraph(ICollection<Connection> connections, IActivity? rootActivity)
{
    private List<Connection>? _cachedForwardConnections;
    private readonly Dictionary<IActivity, List<Connection>> _cachedInboundForwardConnections = new();
    private readonly Dictionary<IActivity, List<Connection>> _cachedInboundConnections = new();
    private readonly Dictionary<IActivity, List<Connection>> _cachedOutboundConnections = new();
    private readonly Dictionary<Connection, (bool IsBackwardConnection, bool IsValid)> _cachedIsBackwardConnection = new();
    private readonly Dictionary<IActivity, bool> _cachedIsDanglingActivity = new();
    private readonly Dictionary<IActivity, List<IActivity>> _cachedAncestors = new();

    /// <summary>
    /// Gets the list of forward connections, computing them if not already cached.
    /// </summary>
    private List<Connection> ForwardConnections => _cachedForwardConnections ??= rootActivity == null ? new() : GetForwardConnections(connections, rootActivity);

    /// <summary>
    /// Retrieves all inbound forward connections for a given activity.
    /// </summary>
    public List<Connection> GetForwardInboundConnections(IActivity activity) => _cachedInboundForwardConnections.GetOrAdd(activity, () => ForwardConnections.InboundConnections(activity).ToList());

    /// <summary>
    /// Retrieves all outbound connections for a given activity.
    /// </summary>
    public List<Connection> GetOutboundConnections(IActivity activity) => _cachedOutboundConnections.GetOrAdd(activity, () => connections.OutboundConnections(activity).ToList());
    
    /// <summary>
    /// Retrieves all inbound connections for a given activity.
    /// </summary>
    public List<Connection> GetInboundConnections(IActivity activity) => _cachedInboundConnections.GetOrAdd(activity, () => connections.InboundConnections(activity).ToList());

    /// <summary>
    /// Determines if a given activity is "dangling," meaning it does not exist as a target in any forward connection.
    /// </summary>
    public bool IsDanglingActivity(IActivity activity) => _cachedIsDanglingActivity.GetOrAdd(activity, () => activity != rootActivity && ForwardConnections.All(c => c.Target.Activity != activity));

    /// <summary>
    /// Determines if a given connection is a backward connection (i.e., not part of the forward traversal) and whether it is valid.
    /// </summary>
    public bool IsBackwardConnection(Connection connection, out bool isValid)
    {
        // Check if result is already cached
        if (_cachedIsBackwardConnection.TryGetValue(connection, out var result))
        {
            isValid = result.IsValid;
            return result.IsBackwardConnection;
        }

        // Compute if the connection is backward
        bool isBackwardConnection = !GetForwardInboundConnections(connection.Target.Activity).Contains(connection);

        // Compute if the backward connection is valid
        isValid = isBackwardConnection && IsValidBackwardConnection(ForwardConnections, rootActivity, connection);

        // Cache the result
        _cachedIsBackwardConnection[connection] = (isBackwardConnection, isValid);

        return isBackwardConnection;
    }

    /// <summary>
    /// Retrieves all ancestor activities for a given activity by traversing ForwardConnections in reverse.
    /// </summary>
    public List<IActivity> GetAncestorActivities(IActivity activity)
    {
        return _cachedAncestors.GetOrAdd(activity, () => ComputeAncestors(activity));
    }

    /// <summary>
    /// Computes the list of ancestors by following Source activities in ForwardConnections.
    /// </summary>
    private List<IActivity> ComputeAncestors(IActivity activity)
    {
        HashSet<IActivity> ancestors = new();
        Queue<IActivity> queue = new();

        // Find all connections where this activity is the target
        foreach (var connection in ForwardConnections.Where(c => c.Target.Activity == activity))
        {
            if (ancestors.Add(connection.Source.Activity))
                queue.Enqueue(connection.Source.Activity);
        }

        // Traverse upwards through the graph
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var connection in ForwardConnections.Where(c => c.Target.Activity == current))
            {
                if (ancestors.Add(connection.Source.Activity))
                    queue.Enqueue(connection.Source.Activity);
            }
        }

        return ancestors.ToList();
    }

    /// <summary>
    /// Computes the list of forward connections in the graph, excluding cyclic connections.
    /// </summary>
    private static List<Connection> GetForwardConnections(ICollection<Connection> connections, IActivity root)
    {
        Dictionary<IActivity, List<IActivity>> adjList = new();

        foreach (var conn in connections)
        {
            if (!adjList.ContainsKey(conn.Source.Activity))
                adjList[conn.Source.Activity] = new();

            adjList[conn.Source.Activity].Add(conn.Target.Activity);
        }

        HashSet<IActivity> visited = new();
        HashSet<(IActivity, IActivity)> visitedEdges = new();
        List<(IActivity Source, IActivity Target)> validEdges = new();
        Queue<IActivity> queue = new();

        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var source = queue.Dequeue();
            visited.Add(source);

            if (!adjList.ContainsKey(source)) continue;

            foreach (var target in adjList[source])
            {
                var edge = (source, target);
                if (visitedEdges.Contains(edge))
                    continue;

                if (HasPathToActivity(validEdges, target, source))
                    continue;

                visitedEdges.Add(edge);
                validEdges.Add((source, target));

                if (!visited.Contains(target))
                    queue.Enqueue(target);
            }
        }

        return validEdges
            .SelectMany(e => connections.Where(c => c.Source.Activity == e.Source && c.Target.Activity == e.Target))
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Determines if there is an existing path from the source activity to the target activity.
    /// Helps in detecting cyclic connections.
    /// </summary>
    private static bool HasPathToActivity(ICollection<(IActivity Source, IActivity Target)> edges, IActivity source, IActivity target)
    {
        if (source == target)
            return true;

        HashSet<IActivity> visited = new();
        Stack<IActivity> stack = new();
        stack.Push(source);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (current == target)
                return true;

            if (visited.Contains(current))
                continue;

            visited.Add(current);

            foreach (var next in edges.Where(x => x.Source == current).Select(e => e.Target))
            {
                if (!visited.Contains(next))
                    stack.Push(next);
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a backward connection is valid by ensuring all paths from source to root pass through target.
    /// </summary>
    private static bool IsValidBackwardConnection(List<Connection> forwardConnections, IActivity? root, Connection connection)
    {
        if (root == null) return false;

        var pathsToRoot = GetPathsToRoot(forwardConnections, root, connection.Source.Activity);

        foreach (var path in pathsToRoot)
        {
            if (!path.Contains(connection.Target.Activity))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Finds all paths from a given start activity to the root using BFS.
    /// </summary>
    private static List<List<IActivity>> GetPathsToRoot(List<Connection> forwardConnections, IActivity root, IActivity start)
    {
        List<List<IActivity>> paths = new();
        Queue<List<IActivity>> queue = new();
        queue.Enqueue(new()
            { start });

        while (queue.Count > 0)
        {
            var path = queue.Dequeue();
            var lastNode = path.Last();

            if (lastNode == root)
            {
                paths.Add([..path]);
                continue;
            }

            var previousNodes = forwardConnections
                .Where(c => c.Target.Activity == lastNode)
                .Select(c => c.Source.Activity);

            foreach (var prev in previousNodes)
            {
                if (!path.Contains(prev))
                {
                    var newPath = new List<IActivity>(path) { prev };
                    queue.Enqueue(newPath);
                }
            }
        }

        return paths;
    }
}
