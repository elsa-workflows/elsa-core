using System;
using Elsa.Activities.Webhooks.ActivityTypes;
using Elsa.Activities.Webhooks.Bookmarks;
using Elsa.Activities.Webhooks.Persistence.Decorators;
using Elsa.Services;
using Elsa.Webhooks.Persistence;
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
                .AddBookmarkProvider<WebhookBookmarkProvider>();

            services.Decorate<IWebhookDefinitionStore, InitializingWebhookDefinitionStore>();
            services.Decorate<IWebhookDefinitionStore, EventPublishingWebhookDefinitionStore>();

            return elsaOptions;
        }


    }
}
