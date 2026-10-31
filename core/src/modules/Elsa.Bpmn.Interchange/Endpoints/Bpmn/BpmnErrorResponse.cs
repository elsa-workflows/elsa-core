using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>
/// The error envelope every BPMN-specific refusal coded through <see cref="BpmnErrorCodes"/> is sent as.
/// </summary>
/// <remarks>
/// <para>
/// FastEndpoints has no way, in this deployment's configuration, to surface a
/// <see cref="FluentValidation.Results.ValidationFailure.ErrorCode"/> in the error response it builds by default:
/// that requires either its <c>ProblemDetails</c> response (which this deployment does not use — see
/// <c>Elsa.FastEndpointConfigurators.ElsaFastEndpointsConfigurator</c>) with its <c>IndicateErrorCode</c> flag set,
/// or replacing <c>Config.ErrOpts.ResponseBuilder</c>, which is a single, process-wide FastEndpoints setting — doing
/// so here would reshape every endpoint's error response in the process, not just these BPMN endpoints.
/// </para>
/// <para>
/// So these endpoints write this response themselves, sent through <see cref="SendAsync"/> instead of
/// FastEndpoints' <c>Send.ErrorsAsync</c>, keeping the same top-level shape FastEndpoints' default
/// <c>ErrorResponse</c> would have sent — <see cref="StatusCode"/>, <see cref="Message"/>, and an
/// <see cref="Errors"/> dictionary with the same <c>generalErrors</c> key <c>AddError(message)</c> groups under —
/// so a caller that only reads <c>message</c>/<c>errors</c> today, such as Elsa Studio's
/// <c>ValidationApiExceptionExtensions.GetValidationErrorsFromContent</c>, keeps seeing exactly what it saw before
/// this envelope's two additive members, <see cref="Code"/> and <see cref="Data"/>, existed.
/// </para>
/// </remarks>
internal sealed class BpmnErrorResponse
{
    /// <summary>
    /// The key FastEndpoints' own <c>AddError(message)</c> groups a message-only failure under — camelCased from
    /// its <c>Config.ErrOpts.GeneralErrorsField</c> default of <c>"GeneralErrors"</c>, which nothing in this
    /// deployment overrides (see <c>Elsa.FastEndpointConfigurators.ElsaFastEndpointsConfigurator</c>).
    /// </summary>
    private const string GeneralErrorsKey = "generalErrors";

    /// <summary>The HTTP status code sent to the client.</summary>
    public required int StatusCode { get; init; }

    /// <summary>The same default message FastEndpoints' own <c>ErrorResponse</c> carries when nothing overrides it.</summary>
    public string Message { get; init; } = "One or more errors occurred!";

    /// <summary>The same shape FastEndpoints' own <c>ErrorResponse</c> builds from an endpoint's <c>AddError</c> calls.</summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; init; }

    /// <summary>The stable, machine-readable code identifying this refusal. See <see cref="BpmnErrorCodes"/>.</summary>
    public required string Code { get; init; }

    /// <summary>Structured data specific to <see cref="Code"/> (e.g. the missing capability names and element ids), or <c>null</c> when the code carries none.</summary>
    public object? Data { get; init; }

    /// <summary>Builds the response for a single-message refusal, in the same shape <c>AddError(message)</c> would have produced.</summary>
    public static BpmnErrorResponse Create(string message, string code, int statusCode, object? data = null) => new()
    {
        StatusCode = statusCode,
        Errors = new Dictionary<string, IReadOnlyList<string>> { [GeneralErrorsKey] = [message] },
        Code = code,
        Data = data
    };

    /// <summary>Sends <paramref name="response"/> with its own <see cref="StatusCode"/>.</summary>
    /// <remarks>
    /// Goes through <see cref="HttpResponse"/>'s own <c>SendAsync</c> extension rather than an endpoint's
    /// <c>Send.ErrorsAsync</c>/<c>Send.ResponseAsync</c>, since those build FastEndpoints' own <c>ErrorResponse</c>
    /// (see this type's remarks) or require the response type FastEndpoints generated for the calling endpoint's
    /// declared success response, neither of which fits an envelope with a <c>code</c> and a <c>data</c> member.
    /// <c>HttpResponse.SendAsync</c> still runs through <c>Config.SerOpts.ResponseSerializer</c> — the same
    /// <c>IApiSerializer</c>-backed serializer <c>Elsa.FastEndpointConfigurators.ElsaFastEndpointsConfigurator</c>
    /// configures for every other response — so this response's JSON casing matches the rest of the API.
    /// </remarks>
    public static Task SendAsync(HttpResponse httpResponse, BpmnErrorResponse response, CancellationToken cancellationToken) =>
        httpResponse.SendAsync(response, response.StatusCode, cancellation: cancellationToken);
}
