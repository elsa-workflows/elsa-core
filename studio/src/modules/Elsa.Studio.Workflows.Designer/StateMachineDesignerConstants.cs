using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Designer;

/// <summary>
/// Contains shared constants used by the StateMachine designer.
/// </summary>
public static class StateMachineDesignerConstants
{
    /// <summary>
    /// Gets the validation code used when the StateMachine states value is not a JSON array.
    /// </summary>
    public const string InvalidStateCollectionCode = "InvalidStateCollection";

    /// <summary>
    /// Gets the validation code used when the StateMachine transitions value is not a JSON array.
    /// </summary>
    public const string InvalidTransitionCollectionCode = "InvalidTransitionCollection";

    /// <summary>
    /// Gets the validation code used when a StateMachine state item is not a JSON object.
    /// </summary>
    public const string InvalidStateItemCode = "InvalidStateItem";

    /// <summary>
    /// Gets the validation code used when a StateMachine transition item is not a JSON object.
    /// </summary>
    public const string InvalidTransitionItemCode = "InvalidTransitionItem";

    /// <summary>
    /// Gets the marker property used for slot values that could not be parsed as JSON.
    /// </summary>
    public const string InvalidJsonSlotProperty = "$invalidJson";

    /// <summary>
    /// Gets the marker value used for slot values that could not be parsed as JSON.
    /// </summary>
    public const string InvalidJsonSlotMarkerValue = "Elsa.Studio.Workflows.StateMachineDesigner.InvalidJsonSlot";

    /// <summary>
    /// Gets the property that stores the original invalid JSON text.
    /// </summary>
    public const string InvalidJsonSlotSourceProperty = "source";

    /// <summary>
    /// Returns true when the specified object is the designer-created invalid JSON marker.
    /// </summary>
    public static bool IsInvalidJsonSlotMarker(JsonObject obj) =>
        obj.TryGetPropertyValue(InvalidJsonSlotProperty, out var markerNode) &&
        markerNode is JsonValue markerValue &&
        markerValue.TryGetValue<string>(out var marker) &&
        string.Equals(marker, InvalidJsonSlotMarkerValue, StringComparison.Ordinal) &&
        obj.TryGetPropertyValue(InvalidJsonSlotSourceProperty, out var sourceNode) &&
        sourceNode is JsonValue sourceValue &&
        sourceValue.TryGetValue<string>(out _);
}
