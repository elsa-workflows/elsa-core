namespace Elsa.Workflows.Activities.Flowchart.Models;

/// <summary>
/// Detects which connections in a directed graph point back to an ancestor (i.e. are true loop-back edges).
/// </summary>
public class LoopbackDetector
{
    private enum NodeState { Unvisited, Visiting, Visited }
    private readonly HashSet<Connection> _loopbacks = new();

    /// <summary>
    /// Initializes the detector and immediately runs a DFS
    /// over the given activities + connections to find all loop-back edges.
    /// </summary>
    public LoopbackDetector(
        IEnumerable<IActivity> activities,
        IEnumerable<Connection> connections)
    {
        var activityList = activities.ToList();

        // 1) Build adjacency list by source activity id.
        var adj = connections
            .GroupBy(c => c.Source.Activity.Id)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 2) Initialize all nodes as unvisited.
        var state = activityList
            .ToDictionary(a => a.Id, a => NodeState.Unvisited);

        // 3) Run DFS from each unvisited node
        foreach (var activity in activityList)
            if (state[activity.Id] == NodeState.Unvisited)
                DepthFirstSearch(activity.Id, adj, state);
    }

    /// <summary>
    /// Returns true if this connection was detected as a loop-back edge.
    /// </summary>
    public bool IsLoopback(Connection connection) => _loopbacks.Contains(connection);

    private void DepthFirstSearch(
        string nodeId,
        Dictionary<string, List<Connection>> adjacencyList,
        Dictionary<string, NodeState> state)
    {
        state[nodeId] = NodeState.Visiting;

        if (adjacencyList.TryGetValue(nodeId, out var outgoing))
        {
            foreach (var conn in outgoing)
            {
                var targetId = conn.Target.Activity.Id;

                if (!state.TryGetValue(targetId, out var targetState))
                    continue; // orphan target?

                if (targetState == NodeState.Visiting)
                {
                    // we found a back-edge.
                    _loopbacks.Add(conn);
                }
                else if (targetState == NodeState.Unvisited)
                {
                    DepthFirstSearch(targetId, adjacencyList, state);
                }
            }
        }

        state[nodeId] = NodeState.Visited;
    }
}