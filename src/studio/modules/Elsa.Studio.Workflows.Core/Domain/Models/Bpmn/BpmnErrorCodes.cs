namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The stable, machine-readable codes Studio recognizes on a BPMN interchange refusal's error envelope, mirroring
/// elsa-core's own <c>Elsa.Bpmn.Interchange.BpmnErrorCodes</c> (the source of truth for these values and for exactly
/// which endpoint sends each one, in what shape). See that type, and its accompanying
/// <c>doc/wiki/bpmn-workflows.md</c> "Error codes on refusals" section, for the full envelope this code lives
/// alongside (<c>statusCode</c>, <c>message</c>, <c>errors</c>, plus the additive <c>code</c> and <c>data</c>
/// <see cref="Elsa.Studio.Workflows.Domain.Models.ValidationErrors"/> also carries).
/// </summary>
/// <remarks>
/// <c>RemoteBpmnInterchangeService</c> classifies the export refusals by these codes into
/// <see cref="BpmnExportFailureReason"/>, and the document <c>GET</c>/<c>PUT</c> refusals into
/// <see cref="BpmnDocumentFailureReason"/>; <c>BpmnImportUiService</c> reads <see cref="ImportCapabilityUnsupported"/>.
/// </remarks>
public static class BpmnErrorCodes
{
    /// <summary>
    /// <c>bpmn/import</c> (and the document <c>PUT</c>) refuses a document that needs a BPMN
    /// host capability this deployment does not declare. Carries <c>data.capabilities</c> (the missing capability
    /// names) and <c>data.elementIds</c> (the offending element ids, combined across every missing capability).
    /// </summary>
    public const string ImportCapabilityUnsupported = "bpmn.import.capability-unsupported";

    /// <summary>
    /// <c>bpmn/import</c> (and the document <c>PUT</c>) refuses a document whose work binding cannot be turned into
    /// a runnable Elsa activity.
    /// </summary>
    public const string ImportBindingInvalid = "bpmn.import.binding-invalid";

    /// <summary>
    /// <c>bpmn/definitions/{id}/export</c> (and the document <c>GET</c>) refuses a workflow definition that does not
    /// currently carry BPMN source — either it was never imported from BPMN, or a later save replaced its custom
    /// properties wholesale.
    /// </summary>
    public const string ExportNotImported = "bpmn.export.not-imported";

    /// <summary>
    /// <c>bpmn/definitions/{id}/export</c> (and the document <c>GET</c>) refuses a workflow definition whose stored
    /// BPMN source no longer corresponds to it — its version, or (for an unpublished draft saved in place) its
    /// activity graph, has moved on since the source was recorded.
    /// </summary>
    public const string ExportSourceStale = "bpmn.export.source-stale";

    /// <summary>
    /// <c>bpmn/definitions/{id}/export</c> (and the document <c>GET</c>) refuses a workflow definition that carries
    /// BPMN source but not the definition version it was recorded against, so whether that source is still current
    /// cannot be verified. Not reachable through <c>Import</c> itself; only through custom properties edited or
    /// migrated some other way.
    /// </summary>
    public const string ExportSourceVersionUnknown = "bpmn.export.source-version-unknown";

    /// <summary>
    /// The document <c>PUT</c> refuses to edit a workflow definition that no longer exists.
    /// </summary>
    public const string DocumentNotFound = "bpmn.document.not-found";

    /// <summary>
    /// The document <c>PUT</c> requires an <c>If-Match</c> request header and refuses a request that omits it or
    /// sends the wildcard <c>*</c>.
    /// </summary>
    public const string DocumentPreconditionRequired = "bpmn.document.precondition-required";

    /// <summary>
    /// The document <c>PUT</c> refuses an <c>If-Match</c> header that does not match the workflow definition's
    /// current ETag: the definition was written since the caller last read it.
    /// </summary>
    public const string DocumentPreconditionFailed = "bpmn.document.precondition-failed";
}
