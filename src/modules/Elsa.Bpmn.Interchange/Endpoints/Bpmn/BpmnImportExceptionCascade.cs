using Bpmn.Interchange;
using Bpmn.Semantics;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>
/// The exception-to-status-code mapping shared by every endpoint that imports a BPMN document through
/// <c>BpmnInterchangeDocumentService</c> — <c>Import</c> and the document <c>Put</c> endpoint — plus the identical
/// handling both give a result whose <c>ImportResult</c> did not succeed.
/// </summary>
internal static class BpmnImportExceptionCascade
{
    /// <summary>
    /// Runs <paramref name="import"/>, reporting the shared error response for whichever exception it throws, or for
    /// an unsuccessful <see cref="BpmnDocumentImportResult.ImportResult"/>, through <paramref name="addError"/> and
    /// <paramref name="sendErrorsAsync"/>. Returns <c>null</c> in every case that already sent a response; the caller
    /// sends its own success response otherwise.
    /// </summary>
    public static async Task<BpmnDocumentImportResult?> RunAsync(
        Func<Task<BpmnDocumentImportResult>> import,
        Action<string> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        CancellationToken cancellationToken)
    {
        BpmnDocumentImportResult result;

        try
        {
            result = await import();
        }
        catch (BpmnDefinitionNotFoundException exception)
        {
            addError(exception.Message);
            await sendErrorsAsync(StatusCodes.Status404NotFound, cancellationToken);
            return null;
        }
        catch (BpmnInterchangeException exception)
        {
            addError(exception.Message);
            await sendErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return null;
        }
        catch (BpmnBindingException exception)
        {
            addError(exception.Message);
            await sendErrorsAsync(StatusCodes.Status422UnprocessableEntity, cancellationToken);
            return null;
        }
        catch (BpmnCapabilityException exception)
        {
            addError(BpmnCapabilityErrorFormatter.Format(exception));
            await sendErrorsAsync(StatusCodes.Status422UnprocessableEntity, cancellationToken);
            return null;
        }

        if (!result.ImportResult.Succeeded)
        {
            foreach (var validationError in result.ImportResult.ValidationErrors)
                addError(validationError.Message);

            await sendErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
            return null;
        }

        return result;
    }
}
