using Elsa.Telnyx.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Telnyx.Extensions
{
    /// <summary>
    /// Provides extensions on <see cref="IEndpointRouteBuilder"/>
    /// </summary>
    public static class EndpointsExtensions
    {
        /// <summary>
        /// Maps the specified route to the Telnyx webhook handler.
        /// </summary>
        public static IEndpointConventionBuilder MapTelnyxWebhook(this IEndpointRouteBuilder endpoints, string routePattern = "telnyx-hook")
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
}