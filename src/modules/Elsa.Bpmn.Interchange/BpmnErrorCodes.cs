namespace Elsa.Bpmn.Interchange;

/// <summary>
/// Stable, machine-readable codes carried alongside the human-readable message of every BPMN-specific refusal
/// <c>bpmn/import</c>, <c>bpmn/definitions/{id}/export</c> and the <c>bpmn/definitions/{id}/document</c> GET/PUT
/// endpoints send, in the additive error envelope <see cref="Endpoints.Bpmn.BpmnErrorResponse"/> describes.
/// </summary>
/// <remarks>
/// Studio (and any other caller) matches on these codes rather than on the message text, so a rewording of a
/// message never breaks a caller that only reads the code. Treat every value here as a compatibility surface: once
/// published, a code must not be renamed or reused for a different refusal. See doc/wiki/bpmn-workflows.md's REST
/// endpoints section for the endpoint(s) each code is sent from and what "data" (if any) it carries.
/// </remarks>
public static class BpmnErrorCodes
{
    /// <summary>
    /// <c>bpmn/import</c> and the document <c>PUT</c> refuse a document that needs a BPMN host capability this
    /// deployment does not declare. Carries <c>data.capabilities</c> (the missing capability names) and
    /// <c>data.elementIds</c> (the offending element ids, combined across every missing capability).
    /// </summary>
    public const string ImportCapabilityUnsupported = "bpmn.import.capability-unsupported";

    /// <summary>
    /// <c>bpmn/import</c> and the document <c>PUT</c> refuse a document whose work binding — an
    /// <c>elsa:activityBinding</c>, a timer duration, a call activity — cannot be turned into a runnable Elsa
    /// activity.
    /// </summary>
    public const string ImportBindingInvalid = "bpmn.import.binding-invalid";

    /// <summary>
    /// <c>bpmn/definitions/{id}/export</c> and the document <c>GET</c> refuse a workflow definition that does not
    /// currently carry BPMN source — either it was never imported from BPMN, or a later save replaced its custom
    /// properties wholesale.
    /// </summary>
    public const string ExportNotImported = "bpmn.export.not-imported";

    /// <summary>
    /// <c>bpmn/definitions/{id}/export</c> and the document <c>GET</c> refuse a workflow definition whose stored
    /// BPMN source no longer corresponds to it — its version, or (for an unpublished draft saved in place) its
    /// activity graph, has moved on since the source was recorded.
    /// </summary>
    public const string ExportSourceStale = "bpmn.export.source-stale";

    /// <summary>
    /// <c>bpmn/definitions/{id}/export</c> and the document <c>GET</c> refuse a workflow definition that carries
    /// BPMN source but not the definition version it was recorded against, so whether that source is still current
    /// cannot be verified. Distinct from <see cref="ExportNotImported"/> (the source text is present) and
    /// <see cref="ExportSourceStale"/> (there is no version to compare against yet); not reachable through
    /// <c>Import</c> itself, only through custom properties edited or migrated some other way.
    /// </summary>
    public const string ExportSourceVersionUnknown = "bpmn.export.source-version-unknown";

    /// <summary>
    /// The document <c>PUT</c> refuses to edit a workflow definition that no longer exists — e.g. it was deleted
    /// between the endpoint's own existence/ETag check and the import that follows it.
    /// </summary>
    public const string DocumentNotFound = "bpmn.document.not-found";

    /// <summary>
    /// The document <c>PUT</c> requires an <c>If-Match</c> request header carrying the ETag a prior <c>GET</c>
    /// returned, and refuses a request that omits it or sends the wildcard <c>*</c>.
    /// </summary>
    public const string DocumentPreconditionRequired = "bpmn.document.precondition-required";

    /// <summary>
    /// The document <c>PUT</c> refuses an <c>If-Match</c> header that does not match the workflow definition's
    /// current ETag: the definition was written since the caller last read it.
    /// </summary>
    public const string DocumentPreconditionFailed = "bpmn.document.precondition-failed";
}
