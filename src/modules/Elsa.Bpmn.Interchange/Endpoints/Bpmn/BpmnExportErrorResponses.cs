using Bpmn.Interchange;
using Elsa.Bpmn.Interchange.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>
/// The exception-to-response mapping shared by every endpoint that reads a workflow definition's stored BPMN source
/// back out through <c>BpmnInterchangeDocumentService</c> — <c>Export</c> and the document <c>Get</c> endpoint —
/// refusing the same "missing" and "stale" cases the same way.
/// </summary>
internal static class BpmnExportErrorResponses
{
    /// <summary>
    /// Runs <paramref name="sendResponse"/>, reporting the shared error response for whichever exception it throws.
    /// A <see cref="BpmnExportUnavailableException"/> goes out through <paramref name="httpResponse"/> as a
    /// <see cref="BpmnErrorResponse"/> coded from its <see cref="BpmnExportUnavailableException.Reason"/>; the
    /// remaining, uncoded <see cref="BpmnInterchangeException"/> case still goes through <paramref name="addError"/>
    /// and <paramref name="sendErrorsAsync"/>, exactly as before.
    /// </summary>
    public static async Task RunAsync(
        Func<Task> sendResponse,
        HttpResponse httpResponse,
        Action<string> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            await sendResponse();
        }
        catch (BpmnExportUnavailableException exception)
        {
            await BpmnErrorResponseSender.SendAsync(httpResponse, ResponseFor(exception), cancellationToken);
        }
        catch (BpmnInterchangeException exception)
        {
            addError(exception.Message);
            await sendErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
        }
    }

    /// <summary>
    /// The response for <paramref name="exception"/>, coded from its <see cref="BpmnExportUnavailableException.Reason"/>,
    /// as its own pure, synchronous step so it can be asserted on directly.
    /// <see cref="BpmnExportUnavailableReason.SourceVersionUnknown"/> in particular is not reachable through
    /// <c>Import</c> or the document <c>PUT</c> themselves — both always record a source version alongside the
    /// source text — only through custom properties edited or migrated some other way, so an HTTP-level test cannot
    /// reach it either.
    /// </summary>
    internal static BpmnErrorResponse ResponseFor(BpmnExportUnavailableException exception)
    {
        var code = exception.Reason switch
        {
            BpmnExportUnavailableReason.NotImported => BpmnErrorCodes.ExportNotImported,
            BpmnExportUnavailableReason.SourceVersionUnknown => BpmnErrorCodes.ExportSourceVersionUnknown,
            BpmnExportUnavailableReason.SourceStale => BpmnErrorCodes.ExportSourceStale,
            _ => throw new ArgumentOutOfRangeException(nameof(exception), exception.Reason, "Unknown BpmnExportUnavailableReason.")
        };

        return BpmnErrorResponseFactory.Create(exception.Message, code, StatusCodes.Status422UnprocessableEntity);
    }
}
