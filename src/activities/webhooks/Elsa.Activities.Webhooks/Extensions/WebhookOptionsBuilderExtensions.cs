using System;
using Elsa.Activities.Webhooks.ActivityTypes;
using Elsa.Activities.Webhooks.Persistence.Decorators;
using Elsa.Activities.Webhooks.Services;
using Elsa.Services;
using Elsa.Webhooks.Abstractions.Persistence;
using Elsa.Webhooks.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Webhooks.Extensions
{
    public static class WebhookOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder AddWebhooks(
            this ElsaOptionsBuilder elsaOptions,
            Action<WebhookOptionsBuilder>? configure = default)
        {
            var services = elsaOptions.Services;

            // Configure Webhooks.
            var webhookOptionsBuilder = new WebhookOptionsBuilder(elsaOptions.Services);
            configure?.Invoke(webhookOptionsBuilder);

            // Services.
            
            services
                .AddScoped<IActivityTypeProvider, WebhookActivityTypeProvider>()
                .AddScoped(sp => webhookOptionsBuilder.WebhookOptions.WebhookDefinitionStoreFactory(sp))
                .AddScoped<IWebhookPublisher, WebhookPublisher>();

            services.Decorate<IWebhookDefinitionStore, InitializingWebhookDefinitionStore>();
            services.Decorate<IWebhookDefinitionStore, EventPublishingWebhookDefinitionStore>();

            return elsaOptions;
        }


    }
}
