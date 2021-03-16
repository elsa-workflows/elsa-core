using System;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.ActivityTypes;
using Elsa.Activities.Telnyx.Bookmarks;
using Elsa.Activities.Telnyx.Handlers;
using Elsa.Activities.Telnyx.Options;
using Elsa.Activities.Telnyx.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Telnyx.Extensions
{
    public static class SetupExtensions
    {
        public static ElsaOptions AddTelnyx(this ElsaOptions elsaOptions, Action<TelnyxOptions>? configure = default)
        {
            var services = elsaOptions.Services;
            var telnyxOptions = services.GetTelnyxOptions();

            configure?.Invoke(telnyxOptions);

            services
                .AddActivityTypeProvider<NotificationActivityTypeProvider>()
                .AddBookmarkProvider<NotificationBookmarkProvider>()
                .AddNotificationHandlers(typeof(TriggerWorkflows))
                .AddScoped<IWebhookHandler, WebhookHandler>();

            return elsaOptions;
        }

        public static IEndpointConventionBuilder MapTelnyxWebhook(this IEndpointRouteBuilder endpoints, string routePattern = "telnyx-hook")
        {
            return endpoints.MapPost(routePattern, HandleTelnyxRequest);
        }

        private static TelnyxOptions GetTelnyxOptions(this IServiceCollection services)
        {
            var telnyxOptions = (TelnyxOptions?) services.FirstOrDefault(x => x.ServiceType == typeof(TelnyxOptions))?.ImplementationInstance;

            if (telnyxOptions == null)
            {
                telnyxOptions = new TelnyxOptions();
                services.AddSingleton(telnyxOptions);
            }

            return telnyxOptions;
        }

        private static async Task HandleTelnyxRequest(HttpContext context)
        {
            var services = context.RequestServices;
            var webhookHandler = services.GetRequiredService<IWebhookHandler>();
            await webhookHandler.HandleAsync(context);
        }
    }
}