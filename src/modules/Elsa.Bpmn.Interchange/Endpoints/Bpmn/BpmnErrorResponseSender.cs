using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>Sends a <see cref="BpmnErrorResponse"/> through the same serializer every other Elsa API response uses.</summary>
/// <remarks>
/// Goes through <see cref="HttpResponse"/>'s own <c>SendAsync</c> extension rather than an endpoint's
/// <c>Send.ErrorsAsync</c>/<c>Send.ResponseAsync</c>, since those build FastEndpoints' own <c>ErrorResponse</c> (see
/// <see cref="BpmnErrorResponse"/>'s remarks) or require the response type FastEndpoints generated for the calling
/// endpoint's declared success response, neither of which fits an envelope with a <c>code</c> and a <c>data</c>
/// member. <c>HttpResponse.SendAsync</c> still runs through <c>Config.SerOpts.ResponseSerializer</c> — the same
/// <c>IApiSerializer</c>-backed serializer <c>Elsa.FastEndpointConfigurators.ElsaFastEndpointsConfigurator</c>
/// configures for every other response — so this response's JSON casing matches the rest of the API.
/// </remarks>
internal static class BpmnErrorResponseSender
{
    /// <summary>Sends <paramref name="response"/> with its own <see cref="BpmnErrorResponse.StatusCode"/>.</summary>
    public static Task SendAsync(HttpResponse httpResponse, BpmnErrorResponse response, CancellationToken cancellationToken) =>
        httpResponse.SendAsync(response, response.StatusCode, cancellation: cancellationToken);
}
