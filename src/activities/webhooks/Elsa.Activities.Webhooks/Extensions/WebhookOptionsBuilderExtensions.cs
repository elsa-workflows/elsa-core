using System;
using Elsa.Activities.Webhooks.ActivityTypes;
using Elsa.Activities.Webhooks.Bookmarks;
using Elsa.Activities.Webhooks.Handlers;
using Elsa.Activities.Webhooks.Options;
using Elsa.Activities.Webhooks.Persistence.Decorators;
using Elsa.Options;
using Elsa.Webhooks.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Webhooks.Extensions
{
    public static class WebhookOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder AddWebhooks(this ElsaOptionsBuilder elsaOptions, Action<WebhookOptionsBuilder>? configureOptions = default)
        {
            var services = elsaOptions.Services;
            
            var optionsBuilder = new WebhookOptionsBuilder(services);
            
            services.Configure<WebhookOptions>(webhookOptions =>
            {
                configureOptions?.Invoke(optionsBuilder);
                optionsBuilder.ApplyTo(webhookOptions);
            });

            services
                .AddScoped(sp => sp.GetRequiredService<IOptions<WebhookOptions>>().Value.WebhookDefinitionStoreFactory(sp))
                .AddActivityTypeProvider<WebhookActivityTypeProvider>()
                .AddBookmarkProvider<WebhookBookmarkProvider>()
                .AddNotificationHandlersFrom<EvictWorkflowRegistryCacheHandler>();

            services.Decorate<IWebhookDefinitionStore, InitializingWebhookDefinitionStore>();
            services.Decorate<IWebhookDefinitionStore, EventPublishingWebhookDefinitionStore>();

            return elsaOptions;
        }
    }
}