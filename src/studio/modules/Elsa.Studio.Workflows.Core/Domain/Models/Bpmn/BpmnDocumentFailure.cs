namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// Why a document <c>GET</c> or <c>PUT</c> did not succeed: the <see cref="Reason"/> classified from the error
/// envelope's machine-readable code (see <see cref="BpmnErrorCodes"/>), and the server's own message, which the UI
/// shows as-is wherever it has no wording of its own.
/// </summary>
/// <param name="Reason">The classified reason.</param>
/// <param name="Message">The server's message, or a description of the failure when the server sent none.</param>
/// <param name="CapabilityRefusal">
/// The missing capabilities and offending element ids, for <see cref="BpmnDocumentFailureReason.CapabilityUnsupported"/>.
/// </param>
public sealed record BpmnDocumentFailure(BpmnDocumentFailureReason Reason, string Message, BpmnCapabilityRefusal? CapabilityRefusal = null);

/// <summary>Why a document <c>GET</c> or <c>PUT</c> did not succeed.</summary>
public enum BpmnDocumentFailureReason
{
    /// <summary>The workflow definition does not exist (<c>404</c>, <see cref="BpmnErrorCodes.DocumentNotFound"/>).</summary>
    NotFound,

    /// <summary>
    /// The definition carries no BPMN document to read (<see cref="BpmnErrorCodes.ExportNotImported"/>, or the
    /// rarely reachable <see cref="BpmnErrorCodes.ExportSourceVersionUnknown"/>).
    /// </summary>
    NotImportedFromBpmn,

    /// <summary>
    /// The definition's graph was changed outside BPMN since its document was imported, so the stored document no
    /// longer describes it and cannot be edited until it is re-imported (<see cref="BpmnErrorCodes.ExportSourceStale"/>).
    /// </summary>
    SourceStale,

    /// <summary>The <c>PUT</c> carried no usable <c>If-Match</c> (<c>428</c>, <see cref="BpmnErrorCodes.DocumentPreconditionRequired"/>).</summary>
    PreconditionRequired,

    /// <summary>
    /// The definition was written since the document was read, so the edit was made against a revision that is no
    /// longer current (<c>412</c>, <see cref="BpmnErrorCodes.DocumentPreconditionFailed"/>).
    /// </summary>
    PreconditionFailed,

    /// <summary>The server refused a work binding the document declares (<see cref="BpmnErrorCodes.ImportBindingInvalid"/>).</summary>
    BindingInvalid,

    /// <summary>
    /// The document needs a BPMN host capability the server does not declare
    /// (<see cref="BpmnErrorCodes.ImportCapabilityUnsupported"/>).
    /// </summary>
    CapabilityUnsupported,

    /// <summary>
    /// The <c>GET</c> succeeded but its response carried no <c>ETag</c>, so no edit could ever be written back. Across
    /// origins this means the server's CORS policy does not expose the <c>ETag</c> header.
    /// </summary>
    MissingETag,

    /// <summary>Any other failure; <see cref="BpmnDocumentFailure.Message"/> carries the server's own message.</summary>
    Unknown
}
