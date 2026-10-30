using System.Net;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>How Studio reads the responses of the BPMN document and XML endpoints, which share their refusals and <c>ETag</c> rules.</summary>
internal static class BpmnDocumentResponses
{
    /// <summary>
    /// Classifies a refusal of the document or XML endpoints by the envelope's machine-readable <c>code</c> (see
    /// <see cref="BpmnErrorCodes"/>), never by its wording. A response with no code — a plain <c>404</c>, or an older
    /// server — falls back to the HTTP status where the status alone says what happened, and to
    /// <see cref="BpmnDocumentFailureReason.Unknown"/>, carrying the server's message, otherwise.
    /// </summary>
    public static BpmnDocumentFailure ClassifyFailure(HttpResponseMessage response, string body)
    {
        var errors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(body);
        var message = errors != null
            ? string.Join(" ", errors.Errors.Select(error => error.ErrorMessage))
            : string.IsNullOrWhiteSpace(body) ? response.StatusCode.GetEmptyBodyFailureText(response.ReasonPhrase, $"The server responded with status {(int)response.StatusCode}.") : body;

        var reason = errors?.Code switch
        {
            BpmnErrorCodes.DocumentNotFound => BpmnDocumentFailureReason.NotFound,
            BpmnErrorCodes.DocumentPreconditionRequired => BpmnDocumentFailureReason.PreconditionRequired,
            BpmnErrorCodes.DocumentPreconditionFailed => BpmnDocumentFailureReason.PreconditionFailed,
            BpmnErrorCodes.ImportBindingInvalid => BpmnDocumentFailureReason.BindingInvalid,
            BpmnErrorCodes.ImportCapabilityUnsupported => BpmnDocumentFailureReason.CapabilityUnsupported,
            BpmnErrorCodes.ImportConditionUnsupported => BpmnDocumentFailureReason.ConditionUnsupported,
            BpmnErrorCodes.ExportNotImported or BpmnErrorCodes.ExportSourceVersionUnknown => BpmnDocumentFailureReason.NotImportedFromBpmn,
            BpmnErrorCodes.ExportSourceStale => BpmnDocumentFailureReason.SourceStale,
            _ => response.StatusCode switch
            {
                HttpStatusCode.NotFound => BpmnDocumentFailureReason.NotFound,
                HttpStatusCode.PreconditionFailed => BpmnDocumentFailureReason.PreconditionFailed,
                HttpStatusCode.PreconditionRequired => BpmnDocumentFailureReason.PreconditionRequired,
                _ => BpmnDocumentFailureReason.Unknown
            }
        };

        var capabilityRefusal = reason == BpmnDocumentFailureReason.CapabilityUnsupported ? BpmnCapabilityRefusal.FromData(errors?.Data) : null;
        var conditionRefusal = reason == BpmnDocumentFailureReason.ConditionUnsupported ? BpmnConditionRefusal.FromData(errors?.Data) : null;
        return new(reason, message, capabilityRefusal) { ConditionRefusal = conditionRefusal };
    }

    /// <summary>The <c>ETag</c> header verbatim, quotes included, or <see langword="null"/> when there is none.</summary>
    public static string? ReadETag(HttpResponseMessage response) =>
        response.Headers.TryGetValues("ETag", out var values) ? values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) : null;

    /// <summary>The failure for a document read whose response had no <c>ETag</c>, so no edit to it could be saved.</summary>
    public static BpmnDocumentFailure MissingETag() =>
        new(BpmnDocumentFailureReason.MissingETag, "The server returned the BPMN document without an ETag, so an edit to it could not be saved. If Studio runs on a different origin than the server, the server's CORS policy must expose the ETag header.");
}
