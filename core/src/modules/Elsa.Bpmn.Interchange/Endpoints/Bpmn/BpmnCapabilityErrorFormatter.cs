using Bpmn.Semantics;
using Elsa.Bpmn.Interchange.Services;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>
/// Formats a <see cref="BpmnCapabilityException"/> into the one error message every endpoint that imports a BPMN
/// document — <c>Import</c> and the document <c>Put</c> endpoint — reports it as.
/// </summary>
/// <remarks>
/// <see cref="BpmnCapabilityException"/> carries <see cref="BpmnCapabilityException.DrivingElementIds"/> as a single
/// flat list, already unioned across every missing capability — it does not say which element drove which capability
/// (unlike <c>BpmnCapabilityRequirements.DrivingElementIds</c>, which is per-capability, but that type is gone by the
/// time an endpoint's catch clause sees the exception). Attributing the full, unioned list to each capability
/// individually would put elements next to a capability they may have nothing to do with, so this reports the
/// missing capabilities together with the combined element list once, rather than repeating it.
/// </remarks>
internal static class BpmnCapabilityErrorFormatter
{
    /// <summary>The message an endpoint reports for <paramref name="exception"/>.</summary>
    public static string Format(BpmnCapabilityException exception)
    {
        var missingCapabilities = string.Join(", ", MissingCapabilityNames(exception));
        var elementIds = string.Join(", ", exception.DrivingElementIds);

        return
            $"This deployment does not declare the following BPMN host capabilities the document requires: {missingCapabilities}. "
            + $"Offending elements (combined across all missing capabilities above, not attributable to any one of them): {elementIds}.";
    }

    /// <summary>
    /// The structured <c>data</c> the <see cref="BpmnErrorCodes.ImportCapabilityUnsupported"/> response carries
    /// alongside <see cref="Format"/>'s message: the missing capability names and the offending element ids, under
    /// the same "combined, not attributable to any one capability" caveat <see cref="Format"/>'s remarks explain.
    /// </summary>
    public static object DataFor(BpmnCapabilityException exception) => new
    {
        Capabilities = MissingCapabilityNames(exception),
        ElementIds = exception.DrivingElementIds
    };

    private static IReadOnlyList<string> MissingCapabilityNames(BpmnCapabilityException exception) =>
        BpmnInterchangeDocumentService.IndividualCapabilities
            .Where(capability => exception.Missing.HasFlag(capability))
            .Select(capability => capability.ToString())
            .ToList();
}
