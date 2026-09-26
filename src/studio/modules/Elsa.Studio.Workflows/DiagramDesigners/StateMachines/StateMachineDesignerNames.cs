using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines;

internal static class StateMachineDesignerNames
{
    public static string GetUniqueStateName(StateMachineGraph graph, string requestedName, StateMachineStateNode? excludedState = null)
    {
        var baseName = string.IsNullOrWhiteSpace(requestedName) ? "State" : requestedName.Trim();
        var stateName = baseName;
        var index = 2;
        var names = graph.States
            .Where(x => !ReferenceEquals(x, excludedState))
            .Select(x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        while (names.Contains(stateName))
            stateName = $"{baseName}{index++}";

        return stateName;
    }

    public static string? GetUniqueTransitionName(StateMachineGraph graph, string? requestedName = null, StateMachineTransitionEdge? excludedTransition = null, bool allowNull = false)
    {
        var names = graph.Transitions
            .Where(x => !ReferenceEquals(x, excludedTransition))
            .Select(x => x.Name)
            .Where(x => x != null)
            .Select(x => x!)
            .ToHashSet(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            var baseName = requestedName.Trim();
            var name = baseName;
            var suffix = 2;

            while (names.Contains(name))
                name = $"{baseName}{suffix++}";

            return name;
        }

        if (allowNull)
            return null;

        var index = graph.Transitions.Count + 1;
        var defaultName = $"Transition{index}";

        while (names.Contains(defaultName))
            defaultName = $"Transition{++index}";

        return defaultName;
    }
}
