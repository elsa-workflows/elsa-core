using System.Text.Json.Nodes;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Designer;

namespace Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;

internal enum StateMachineActivityState
{
    Empty,
    Configured,
    Malformed
}

internal sealed record StateMachineActivityPresentation(
    StateMachineActivityState State,
    string Title,
    string Detail,
    string RawDefinition)
{
    public static StateMachineActivityPresentation Describe(JsonNode? value, ILocalizer localizer)
    {
        if (value == null)
            return new(StateMachineActivityState.Empty, string.Empty, string.Empty, string.Empty);

        var rawDefinition = StateMachinePresentationFormatter.JsonSlot(value);
        if (value is not JsonObject obj)
            return new(StateMachineActivityState.Malformed, localizer["Invalid activity definition"], localizer["Expected an activity object."], rawDefinition);

        if (StateMachineDesignerConstants.IsInvalidJsonSlotMarker(obj))
            return new(StateMachineActivityState.Malformed, localizer["Invalid activity definition"], localizer["The original source is preserved."], rawDefinition);

        var typeName = GetString(obj, "type");
        if (string.IsNullOrWhiteSpace(typeName))
            return new(StateMachineActivityState.Malformed, localizer["Unavailable activity definition"], localizer["The activity type is missing."], rawDefinition);

        if (string.IsNullOrWhiteSpace(GetString(obj, "id")) || string.IsNullOrWhiteSpace(GetString(obj, "nodeId")))
            return new(StateMachineActivityState.Malformed, localizer["Incomplete activity definition"], localizer["An activity id and node ID are required."], rawDefinition);

        var title = FirstNonEmpty(GetString(obj, "displayName"), GetString(obj, "name"), typeName)!;
        var version = GetString(obj, "version");
        var detail = string.IsNullOrWhiteSpace(version) ? typeName : $"{typeName} · v{version}";
        return new(StateMachineActivityState.Configured, localizer[title], detail, rawDefinition);
    }

    private static string? GetString(JsonObject obj, string propertyName) =>
        obj[propertyName] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
