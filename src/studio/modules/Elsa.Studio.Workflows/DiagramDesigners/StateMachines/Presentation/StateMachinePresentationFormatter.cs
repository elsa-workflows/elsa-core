using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer;
using Elsa.Studio.Workflows.Designer.Models;
using static Elsa.Studio.Workflows.Designer.StateMachineDesignerConstants;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;

internal static class StateMachinePresentationFormatter
{
    public static string DisplayValue(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    public static string TransitionName(StateMachineTransitionEdge transition) =>
        DisplayValue(FirstNonEmpty(transition.DisplayName, transition.Name));

    public static string JsonSlot(JsonNode? node) =>
        node is JsonObject obj && StateMachineDesignerConstants.IsInvalidJsonSlotMarker(obj)
            ? obj[InvalidJsonSlotSourceProperty]?.GetValue<string>() ?? ""
            : node?.ToJsonString(new() { WriteIndented = true }) ?? "";

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
