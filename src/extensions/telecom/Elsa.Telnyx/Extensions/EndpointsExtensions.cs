using Elsa.Telnyx.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions on <see cref="IEndpointRouteBuilder"/>
/// </summary>
[PublicAPI]
public static class EndpointsExtensions
{
    /// <summary>
    /// Maps the specified route to the Telnyx webhook handler.
    /// </summary>
    public static IEndpointConventionBuilder UseTelnyxWebhooks(this IEndpointRouteBuilder endpoints, string routePattern = "telnyx-hook")
    {
        return endpoints.MapPost(routePattern, HandleTelnyxRequest);
    }

    private static async Task HandleTelnyxRequest(HttpContext context)
    {
        var services = context.RequestServices;
        var webhookHandler = services.GetRequiredService<IWebhookHandler>();
        await webhookHandler.HandleAsync(context);
    }
}