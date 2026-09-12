using Bpmn.Interchange;
using Bpmn.Semantics;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Bpmn.Interchange.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>
/// The exception-to-response mapping shared by every endpoint that imports a BPMN document through
/// <c>BpmnInterchangeDocumentService</c> — <c>Import</c> and the document <c>Put</c> endpoint — plus the identical
/// handling both give a result whose <c>ImportResult</c> did not succeed.
/// </summary>
internal static class BpmnImportErrorResponses
{
    /// <summary>
    /// Runs <paramref name="import"/>, reporting the shared error response for whichever exception it throws, or for
    /// an unsuccessful <see cref="BpmnDocumentImportResult.ImportResult"/>. A refusal this type gives a
    /// <see cref="BpmnErrorCodes"/> code goes out through <paramref name="httpResponse"/> as a
    /// <see cref="BpmnErrorResponse"/>; the remaining, uncoded refusals still go through <paramref name="addError"/>
    /// and <paramref name="sendErrorsAsync"/>, exactly as before. Returns <c>null</c> in every case that already sent
    /// a response; the caller sends its own success response otherwise.
    /// </summary>
    public static async Task<BpmnDocumentImportResult?> RunAsync(
        Func<Task<BpmnDocumentImportResult>> import,
        HttpResponse httpResponse,
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
            await BpmnErrorResponse.SendAsync(httpResponse, NotFoundResponseFor(exception), cancellationToken);
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
            await BpmnErrorResponse.SendAsync(httpResponse, BindingInvalidResponseFor(exception), cancellationToken);
            return null;
        }
        catch (BpmnCapabilityException exception)
        {
            await BpmnErrorResponse.SendAsync(httpResponse, CapabilityResponseFor(exception), cancellationToken);
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

    /// <summary>
    /// The <see cref="BpmnErrorCodes.DocumentNotFound"/> response for <paramref name="exception"/>, as its own pure,
    /// synchronous step so it can be asserted on directly: the endpoints that can throw
    /// <see cref="BpmnDefinitionNotFoundException"/> already refuse the ordinary "no such definition" case earlier,
    /// through their own existence check, so reaching this from a real request needs the definition to be deleted in
    /// the narrow window between that check and the import call this wraps — not something an HTTP-level test can
    /// reliably force without a race.
    /// </summary>
    internal static BpmnErrorResponse NotFoundResponseFor(BpmnDefinitionNotFoundException exception) =>
        BpmnErrorResponse.Create(exception.Message, BpmnErrorCodes.DocumentNotFound, StatusCodes.Status404NotFound);

    /// <summary>The <see cref="BpmnErrorCodes.ImportBindingInvalid"/> response for <paramref name="exception"/>.</summary>
    internal static BpmnErrorResponse BindingInvalidResponseFor(BpmnBindingException exception) =>
        BpmnErrorResponse.Create(exception.Message, BpmnErrorCodes.ImportBindingInvalid, StatusCodes.Status422UnprocessableEntity);

    /// <summary>
    /// The <see cref="BpmnErrorCodes.ImportCapabilityUnsupported"/> response for <paramref name="exception"/>, as its
    /// own pure, synchronous step so it can be asserted on directly: <c>Bpmn.*</c> 0.2.0 declares every capability
    /// this deployment's runtime needs, so nothing in <c>Elsa.Bpmn.Interchange.IntegrationTests</c> can currently
    /// make a real import throw <see cref="BpmnCapabilityException"/> to exercise this through the endpoint itself.
    /// </summary>
    internal static BpmnErrorResponse CapabilityResponseFor(BpmnCapabilityException exception) =>
        BpmnErrorResponse.Create(
            BpmnCapabilityErrorFormatter.Format(exception),
            BpmnErrorCodes.ImportCapabilityUnsupported,
            StatusCodes.Status422UnprocessableEntity,
            BpmnCapabilityErrorFormatter.DataFor(exception));
}
