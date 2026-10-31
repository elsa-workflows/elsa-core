using System.Text.Json;

namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The capability names and offending element ids a BPMN import's <c>422</c> <see cref="BpmnErrorCodes.ImportCapabilityUnsupported"/>
/// refusal carries, read from the error envelope's <c>data.capabilities</c> and <c>data.elementIds</c> (see
/// <see cref="Elsa.Studio.Workflows.Domain.Extensions.ValidationApiExceptionExtensions.GetValidationErrorsFromContent"/>),
/// so the UI can list each separately instead of showing the raw message.
/// </summary>
public sealed record BpmnCapabilityRefusal(IReadOnlyList<string> CapabilityNames, IReadOnlyList<string> ElementIds)
{
    /// <summary>
    /// Reads <paramref name="dataElement"/> (a <see cref="Models.ValidationErrors.Data"/> carried alongside
    /// <see cref="BpmnErrorCodes.ImportCapabilityUnsupported"/>) into a <see cref="BpmnCapabilityRefusal"/> when it
    /// carries a <c>capabilities</c> and/or <c>elementIds</c> string array — the only shape any <c>data</c> member
    /// sends today — or <see langword="null"/> for any other shape, including a code that carries no <c>data</c> at
    /// all.
    /// </summary>
    public static BpmnCapabilityRefusal? FromData(JsonElement? dataElement)
    {
        if (dataElement is not { ValueKind: JsonValueKind.Object } element)
            return null;

        var capabilityNames = TryGetProperty(element, "capabilities", out var capabilitiesElement) ? GetStringArray(capabilitiesElement) : [];
        var elementIds = TryGetProperty(element, "elementIds", out var elementIdsElement) ? GetStringArray(elementIdsElement) : [];

        return capabilityNames.Count == 0 && elementIds.Count == 0 ? null : new BpmnCapabilityRefusal(capabilityNames, elementIds);
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement element) => element.ValueKind == JsonValueKind.Array
        ? element.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList()
        : [];

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in element.EnumerateObject())
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
        }

        property = default;
        return false;
    }
}
