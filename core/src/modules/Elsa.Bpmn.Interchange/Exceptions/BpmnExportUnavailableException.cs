namespace Elsa.Bpmn.Interchange.Exceptions;

/// <summary>
/// Thrown when a workflow definition cannot be exported as BPMN 2.0 XML because the source this deployment would
/// need to write back out is missing or no longer trustworthy.
/// </summary>
/// <remarks>
/// See <see cref="Services.BpmnInterchangeDocumentService"/>'s remarks for the distinct situations this covers — a
/// definition never imported from BPMN (or one whose source a later save dropped), one whose source predates the
/// version marker needed to check it is still current, and one that has changed since it was imported — and why
/// each gets its own message and its own <see cref="Reason"/>, which is what
/// <c>Endpoints.Bpmn.BpmnExportErrorResponses</c> maps to the response's <see cref="Elsa.Bpmn.Interchange.BpmnErrorCodes"/> code
/// without having to parse the message.
/// </remarks>
public class BpmnExportUnavailableException : Exception
{
    /// <summary>Creates the exception with <see cref="BpmnExportUnavailableReason.NotImported"/> as its reason.</summary>
    public BpmnExportUnavailableException(string message) : this(message, BpmnExportUnavailableReason.NotImported)
    {
    }

    /// <summary>Creates the exception with an explicit <paramref name="reason"/>.</summary>
    public BpmnExportUnavailableException(string message, BpmnExportUnavailableReason reason) : base(message)
    {
        Reason = reason;
    }

    /// <summary>Which of the situations this type's remarks describe <see cref="Exception.Message"/> reports.</summary>
    public BpmnExportUnavailableReason Reason { get; }
}

/// <summary>The distinct situations a <see cref="BpmnExportUnavailableException"/> reports.</summary>
public enum BpmnExportUnavailableReason
{
    /// <summary>The definition does not currently carry BPMN source at all.</summary>
    NotImported,

    /// <summary>
    /// The definition carries BPMN source but not the definition version it was recorded against, so whether that
    /// source is still current cannot be verified.
    /// </summary>
    SourceVersionUnknown,

    /// <summary>The definition has changed — by version, or (for an unpublished draft) by activity graph — since the source was recorded.</summary>
    SourceStale
}
