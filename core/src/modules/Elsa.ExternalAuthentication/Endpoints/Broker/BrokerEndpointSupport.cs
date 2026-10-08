using Elsa.ExternalAuthentication.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Elsa.ExternalAuthentication.Endpoints.Broker;

internal static class BrokerEndpointSupport
{
    public static Task SendErrorAsync(IResponseSender sender, PublicBrokerError error, CancellationToken cancellationToken)
    {
        sender.HttpContext.Response.StatusCode = error.Error switch
        {
            "authentication_failed" or "access_denied" => StatusCodes.Status401Unauthorized,
            "method_unavailable" => StatusCodes.Status404NotFound,
            "temporarily_unavailable" => StatusCodes.Status503ServiceUnavailable,
            "rate_limited" => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status400BadRequest
        };
        return sender.HttpContext.Response.WriteAsJsonAsync(error, cancellationToken);
    }
}
