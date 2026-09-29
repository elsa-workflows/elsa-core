using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer;
using Elsa.Studio.Workflows.Designer.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;

public partial class StateMachineTransitionInspector
{
    private static readonly IReadOnlyCollection<string> DefaultKnownExpressionProviderTypes = ["JavaScript"];
    private readonly string _id = $"state-machine-transition-inspector-{Guid.NewGuid():N}";

    [Parameter] public StateMachineTransitionEdge? Transition { get; set; }
    [Parameter] public IReadOnlyCollection<StateMachineStateNode> States { get; set; } = [];
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public string? SelectedActivityId { get; set; }
    [Parameter] public bool TriggerSupportsDesigner { get; set; }
    [Parameter] public bool ActionSupportsDesigner { get; set; }
    [Parameter] public IReadOnlyCollection<StateMachineValidationIssue> ValidationIssues { get; set; } = [];
    [Parameter] public IReadOnlyCollection<string> KnownExpressionProviderTypes { get; set; } = DefaultKnownExpressionProviderTypes;
    [Parameter] public EventCallback<string?> NameChanged { get; set; }
    [Parameter] public EventCallback<string?> DisplayNameChanged { get; set; }
    [Parameter] public EventCallback<string> FromChanged { get; set; }
    [Parameter] public EventCallback<string> ToChanged { get; set; }

    // Retained for compatibility with the original wrapper contract. New callers should use
    // the semantic activity events below so the wrapper can choose the appropriate mutation path.
    [Parameter] public EventCallback<StateMachineSlotValueChange> SlotChanged { get; set; }
    [Parameter] public EventCallback<string> SlotCleared { get; set; }
    [Parameter] public EventCallback<string> SlotDropRequested { get; set; }

    [Parameter] public EventCallback<string> ActivityAddRequested { get; set; }
    [Parameter] public EventCallback<string> ActivitySelectRequested { get; set; }
    [Parameter] public EventCallback<string> ActivityOpenRequested { get; set; }
    [Parameter] public EventCallback<string> ActivityJsonRequested { get; set; }
    [Parameter] public EventCallback<string> ActivityReplaceRequested { get; set; }
    [Parameter] public EventCallback<string> ActivityClearRequested { get; set; }
    [Parameter] public EventCallback<string> ActivityDropRequested { get; set; }
    [Parameter] public EventCallback<string> ConditionEditRequested { get; set; }
    [Parameter] public EventCallback DeleteRequested { get; set; }

    private string HeadingId => $"{_id}-heading";
    private string TriggerStageId => $"{_id}-trigger-stage";
    private string ConditionStageId => $"{_id}-condition-stage";
    private string ActionStageId => $"{_id}-action-stage";
    private string DestinationStageId => $"{_id}-destination-stage";
    private string NameId => $"{_id}-name";
    private string DisplayNameId => $"{_id}-display-name";
    private string FromId => $"{_id}-from";
    private string ToId => $"{_id}-to";

    private Task ChangeNameAsync(ChangeEventArgs args) => NameChanged.InvokeAsync(NormalizeOptionalValue(args.Value));
    private Task ChangeDisplayNameAsync(ChangeEventArgs args) => DisplayNameChanged.InvokeAsync(NormalizeOptionalValue(args.Value));
    private Task ChangeFromAsync(ChangeEventArgs args) => FromChanged.InvokeAsync(args.Value?.ToString() ?? "");
    private Task ChangeToAsync(ChangeEventArgs args) => ToChanged.InvokeAsync(args.Value?.ToString() ?? "");

    private Task RequestConditionEditAsync() => ConditionEditRequested.InvokeAsync("condition");

    private Task RequestActivityAddAsync(string slotName) => ActivityAddRequested.InvokeAsync(slotName);
    private Task RequestActivitySelectAsync(string slotName) => ActivitySelectRequested.InvokeAsync(slotName);
    private Task RequestActivityOpenAsync(string slotName) => ActivityOpenRequested.InvokeAsync(slotName);
    private Task RequestActivityJsonAsync(string slotName) => ActivityJsonRequested.InvokeAsync(slotName);
    private Task RequestActivityReplaceAsync(string slotName) => ActivityReplaceRequested.InvokeAsync(slotName);

    private Task RequestActivityClearAsync(string slotName) => ActivityClearRequested.HasDelegate
        ? ActivityClearRequested.InvokeAsync(slotName)
        : SlotCleared.InvokeAsync(slotName);

    private Task RequestActivityDropAsync(string slotName) => ActivityDropRequested.HasDelegate
        ? ActivityDropRequested.InvokeAsync(slotName)
        : SlotDropRequested.InvokeAsync(slotName);

    private static string? NormalizeOptionalValue(object? value)
    {
        var text = value?.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private bool IsActivitySelected(JsonNode? activity) => activity is JsonObject obj &&
        (string.Equals(obj.GetId(), SelectedActivityId, StringComparison.Ordinal) || string.Equals(obj.GetNodeId(), SelectedActivityId, StringComparison.Ordinal));

    private ConditionPresentation DescribeCondition(JsonNode? value)
    {
        var description = BooleanConditionAdapter.Inspect(value, KnownExpressionProviderTypes ?? DefaultKnownExpressionProviderTypes);
        return description.Kind switch
        {
            BooleanConditionKind.Missing => new(
                ConditionState.Missing,
                Localizer["Always"],
                Localizer["No condition configured; this transition evaluates as true."],
                string.Empty,
                string.Empty),
            BooleanConditionKind.Literal => BooleanCondition(
                description.LiteralValue == true,
                description.LiteralValue == true ? Localizer["Explicitly enabled."] : Localizer["Explicitly disabled."]),
            BooleanConditionKind.Expression => new(
                ConditionState.Expression,
                Localizer[description.ExpressionType ?? "Expression"],
                description.ExpressionValue ?? string.Empty,
                string.Empty,
                string.Empty),
            BooleanConditionKind.Unknown => new(
                ConditionState.Unknown,
                string.IsNullOrWhiteSpace(description.ExpressionType) ? Localizer["Custom condition"] : Localizer["Unavailable condition"],
                string.IsNullOrWhiteSpace(description.ExpressionType)
                    ? Localizer["A custom definition is preserved."]
                    : $"{description.ExpressionType} · {Localizer["definition preserved"]}",
                StateMachinePresentationFormatter.JsonSlot(value),
                string.Empty),
            _ => new(
                ConditionState.Malformed,
                Localizer["Invalid condition definition"],
                Localizer["The original source is preserved."],
                StateMachinePresentationFormatter.JsonSlot(value),
                Localizer["This condition cannot be evaluated until it is repaired or replaced."])
        };
    }

    private ConditionPresentation BooleanCondition(bool value, string detail) => value
        ? new(ConditionState.Always, Localizer["Always"], detail, string.Empty, string.Empty)
        : new(ConditionState.Never, Localizer["Never"], detail, string.Empty, Localizer["This transition cannot pass while the condition is false."]);

    private static string? GetString(JsonObject obj, string propertyName) => obj[propertyName] is JsonValue value && value.TryGetValue<string>(out var text)
        ? text
        : null;

    private enum ConditionState
    {
        Missing,
        Always,
        Never,
        Expression,
        Unknown,
        Malformed
    }

    private sealed record ConditionPresentation(ConditionState State, string Title, string Detail, string RawDefinition, string Warning);
}
