using Bpmn.Interchange;
using Elsa.Bpmn.Interchange.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>
/// The exception-to-status-code mapping shared by every endpoint that reads a workflow definition's stored BPMN
/// source back out through <c>BpmnInterchangeDocumentService</c> — <c>Export</c> and the document <c>Get</c>
/// endpoint — refusing the same "missing" and "stale" cases the same way.
/// </summary>
internal static class BpmnExportExceptionCascade
{
    /// <summary>
    /// Runs <paramref name="sendResponse"/>, reporting the shared error response through <paramref name="addError"/>
    /// and <paramref name="sendErrorsAsync"/> for whichever exception it throws.
    /// </summary>
    public static async Task RunAsync(
        Func<Task> sendResponse,
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
            addError(exception.Message);
            await sendErrorsAsync(StatusCodes.Status422UnprocessableEntity, cancellationToken);
        }
        catch (BpmnInterchangeException exception)
        {
            addError(exception.Message);
            await sendErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
        }
    }
}
