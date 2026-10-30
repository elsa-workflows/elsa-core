using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Designer.Models;
using static Elsa.Studio.Workflows.Designer.StateMachineDesignerConstants;

namespace Elsa.Studio.Workflows.Designer.Services;

/// <summary>
/// Validates StateMachine designer graphs.
/// </summary>
public class StateMachineValidator
{
    /// <summary>
    /// Validates the specified graph.
    /// </summary>
    public IReadOnlyCollection<StateMachineValidationIssue> Validate(StateMachineGraph graph)
    {
        var issues = new List<StateMachineValidationIssue>();
        var names = graph.States.Select(x => x.Name).ToList();
        var nameCounts = names.GroupBy(x => x, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);
        var transitionIdentityCounts = graph.Transitions
            .GroupBy(GetTransitionIdentity)
            .ToDictionary(x => x.Key, x => x.Count());
        var reportedDuplicateTransitionIdentities = new HashSet<(string? Name, string From, string To)>();

        foreach (var state in graph.States)
        {
            if (string.IsNullOrWhiteSpace(state.Name))
                issues.Add(Error("EmptyStateName", "State name is required.", "state"));
            else if (nameCounts[state.Name] > 1)
                issues.Add(Error("DuplicateStateName", $"State name '{state.Name}' is used more than once.", state.Name));

            AddActivitySlotIssue(issues, state.Entry, $"{state.Name}.entry");
            AddActivitySlotIssue(issues, state.Exit, $"{state.Name}.exit");
        }

        foreach (var transition in graph.Transitions)
        {
            var transitionTarget = GetTransitionTarget(transition);

            if (string.IsNullOrWhiteSpace(transition.From))
                issues.Add(Error("MissingTransitionSource", "Transition source state is required.", transitionTarget));
            else if (!nameCounts.TryGetValue(transition.From, out var sourceCount))
                issues.Add(Error("MissingTransitionSourceState", $"Transition source state '{transition.From}' does not exist.", transitionTarget));
            else if (sourceCount > 1)
                issues.Add(Error("AmbiguousTransitionSourceState", $"Transition source state '{transition.From}' matches multiple states.", transitionTarget));

            if (string.IsNullOrWhiteSpace(transition.To))
                issues.Add(Error("MissingTransitionTarget", "Transition target state is required.", transitionTarget));
            else if (!nameCounts.TryGetValue(transition.To, out var targetCount))
                issues.Add(Error("MissingTransitionTargetState", $"Transition target state '{transition.To}' does not exist.", transitionTarget));
            else if (targetCount > 1)
                issues.Add(Error("AmbiguousTransitionTargetState", $"Transition target state '{transition.To}' matches multiple states.", transitionTarget));

            var transitionIdentity = GetTransitionIdentity(transition);
            if (transitionIdentityCounts[transitionIdentity] > 1 && reportedDuplicateTransitionIdentities.Add(transitionIdentity))
                issues.Add(Error("DuplicateTransitionIdentity", $"Transition '{transitionTarget}' has the same name, source, and target as another transition.", transitionTarget));

            AddActivitySlotIssue(issues, transition.Trigger, $"{transitionTarget}.trigger");
            AddInvalidJsonSlotIssue(issues, transition.Condition, $"{transitionTarget}.condition");
            AddActivitySlotIssue(issues, transition.Action, $"{transitionTarget}.action");
        }

        if (!string.IsNullOrWhiteSpace(graph.InitialState) && !nameCounts.ContainsKey(graph.InitialState))
            issues.Add(Warning("MissingInitialState", $"Initial state '{graph.InitialState}' does not exist.", graph.InitialState));

        if (!string.IsNullOrWhiteSpace(graph.CurrentState) && !nameCounts.ContainsKey(graph.CurrentState))
            issues.Add(Warning("MissingCurrentState", $"Current state '{graph.CurrentState}' does not exist.", graph.CurrentState));

        return issues;
    }

    private static string GetTransitionTarget(StateMachineTransitionEdge transition) =>
        NormalizeOptionalString(transition.DisplayName) ?? NormalizeOptionalString(transition.Name) ?? $"{transition.From}->{transition.To}";

    private static (string? Name, string From, string To) GetTransitionIdentity(StateMachineTransitionEdge transition) =>
        (transition.Name, transition.From, transition.To);

    private static string? NormalizeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static void AddInvalidJsonSlotIssue(ICollection<StateMachineValidationIssue> issues, JsonNode? slot, string target)
    {
        if (slot is JsonObject obj && IsInvalidJsonSlotMarker(obj))
            issues.Add(Error("InvalidSlotJson", $"Slot '{target}' contains invalid JSON.", target));
    }

    private static void AddActivitySlotIssue(ICollection<StateMachineValidationIssue> issues, JsonNode? slot, string target)
    {
        AddInvalidJsonSlotIssue(issues, slot, target);

        if (slot == null || slot is JsonObject obj && IsInvalidJsonSlotMarker(obj))
            return;

        if (slot is not JsonObject activity)
        {
            issues.Add(Error("InvalidActivitySlot", $"Slot '{target}' must contain an activity object.", target));
            return;
        }

        if (string.IsNullOrWhiteSpace(activity.GetTypeName()))
        {
            issues.Add(Error("InvalidActivitySlot", $"Slot '{target}' must contain an activity object.", target));
            return;
        }

        var issueCount = issues.Count;

        if (string.IsNullOrWhiteSpace(activity.GetId()))
            issues.Add(Error("MissingActivitySlotId", $"Slot '{target}' activity must include an id.", target));

        if (string.IsNullOrWhiteSpace(activity.GetNodeId()))
            issues.Add(Error("MissingActivitySlotNodeId", $"Slot '{target}' activity must include a nodeId.", target));

        if (issues.Count == issueCount && !activity.IsActivity())
            issues.Add(Error("InvalidActivitySlot", $"Slot '{target}' must contain an activity object.", target));
    }

    private static StateMachineValidationIssue Error(string code, string message, string? target) =>
        new() { Severity = StateMachineValidationSeverity.Error, Code = code, Message = message, Target = target };

    private static StateMachineValidationIssue Warning(string code, string message, string? target) =>
        new() { Severity = StateMachineValidationSeverity.Warning, Code = code, Message = message, Target = target };
}
