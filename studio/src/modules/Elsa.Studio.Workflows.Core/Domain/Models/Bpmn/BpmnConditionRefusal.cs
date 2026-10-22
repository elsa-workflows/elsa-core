using System.Text.Json;

namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The sequence flows a BPMN import's or document <c>PUT</c>'s <c>422</c> <see cref="BpmnErrorCodes.ImportConditionUnsupported"/>
/// refusal names, read from the error envelope's <c>data.elementIds</c>, so the UI can list each flow whose expression
/// condition the server does not evaluate instead of showing the raw message.
/// </summary>
/// <param name="FlowIds">The ids of the sequence flows that carry an expression condition, as the server sent them.</param>
public sealed record BpmnConditionRefusal(IReadOnlyList<string> FlowIds)
{
    /// <summary>
    /// Reads <paramref name="dataElement"/> (a <see cref="Models.ValidationErrors.Data"/> carried alongside
    /// <see cref="BpmnErrorCodes.ImportConditionUnsupported"/>) into a <see cref="BpmnConditionRefusal"/> when it carries
    /// a non-empty <c>elementIds</c> string array, or <see langword="null"/> for any other shape, including no
    /// <c>data</c> at all.
    /// </summary>
    public static BpmnConditionRefusal? FromData(JsonElement? dataElement)
    {
        if (dataElement is not { ValueKind: JsonValueKind.Object } element)
            return null;

        var elementIds = element.EnumerateObject()
            .Where(property => string.Equals(property.Name, "elementIds", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.Array)
            .SelectMany(property => property.Value.EnumerateArray())
            .Where(id => id.ValueKind == JsonValueKind.String)
            .Select(id => id.GetString()!)
            .ToList();

        return elementIds.Count == 0 ? null : new BpmnConditionRefusal(elementIds);
    }
}
