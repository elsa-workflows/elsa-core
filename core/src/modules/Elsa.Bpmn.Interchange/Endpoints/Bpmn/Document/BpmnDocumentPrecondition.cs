using Elsa.Workflows.Management.Entities;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document;

/// <summary>
/// The <c>If-Match</c> check the document and XML <c>PUT</c> endpoints run before they import anything, so both refuse
/// a missing or stale validator the same way.
/// </summary>
/// <remarks>
/// A client that GETs the document, then PUTs it back after someone else has written the definition in between, must
/// not silently replace that intervening write. The client is required to carry the ETag its GET returned as
/// <c>If-Match</c>. A missing header cannot express "I know what I'm overwriting" at all, and neither can "*", which
/// matches whatever is stored — so both are refused as 428 rather than honoured. Anything else must be exactly the
/// current strong ETag (a weak W/ tag or a list never is), or it proves the client's copy is no longer current. The
/// header is checked here so a stale client is refused before import work runs, and again inside the import's
/// compare-and-swap so a write that lands in that window is 412, not a silent overwrite.
/// </remarks>
internal static class BpmnDocumentPrecondition
{
    /// <summary>
    /// Returns the request's <c>If-Match</c> value when it is exactly <paramref name="definition"/>'s current ETag.
    /// Otherwise sends the 428 or 412 refusal and returns <c>null</c>.
    /// </summary>
    public static async Task<string?> CheckAsync(HttpContext httpContext, WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        var ifMatch = httpContext.Request.Headers.IfMatch.ToString().Trim();

        if (ifMatch is "" or "*")
        {
            await BpmnErrorResponse.SendAsync(
                httpContext.Response,
                BpmnErrorResponse.Create(
                    "An If-Match header carrying the ETag from a prior GET of this document is required to PUT it back, so an intervening edit is not silently overwritten. The wildcard \"*\" is not accepted.",
                    BpmnErrorCodes.DocumentPreconditionRequired,
                    StatusCodes.Status428PreconditionRequired),
                cancellationToken);
            return null;
        }

        if (!string.Equals(ifMatch, BpmnDocumentETag.From(definition), StringComparison.Ordinal))
        {
            await BpmnErrorResponse.SendAsync(
                httpContext.Response,
                BpmnErrorResponse.Create(
                    "The workflow definition has been written since the ETag in If-Match was issued. GET the document again, reapply the edit, and PUT it with the new ETag.",
                    BpmnErrorCodes.DocumentPreconditionFailed,
                    StatusCodes.Status412PreconditionFailed),
                cancellationToken);
            return null;
        }

        return ifMatch;
    }
}
