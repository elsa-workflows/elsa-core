using System;
using Elsa.Activities.Webhooks.ActivityTypes;
using Elsa.Activities.Webhooks.Bookmarks;
using Elsa.Activities.Webhooks.Handlers;
using Elsa.Activities.Webhooks.Options;
using Elsa.Activities.Webhooks.Persistence.Decorators;
using Elsa.Extensions;
using Elsa.Options;
using Elsa.Webhooks.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Webhooks.Extensions
{
    public static class WebhookOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder AddWebhooks(this ElsaOptionsBuilder elsaOptions, Action<WebhookOptionsBuilder>? configureOptions = default)
        {
            var services = elsaOptions.Services;
            
            var optionsBuilder = new WebhookOptionsBuilder(services, elsaOptions.ContainerBuilder);
            configureOptions?.Invoke(optionsBuilder);
            var options = optionsBuilder.WebhookOptions;

            elsaOptions.ContainerBuilder.AddMultiton(options);

            elsaOptions.ContainerBuilder
                .AddScoped(sp => sp.GetRequiredService<WebhookOptions>().WebhookDefinitionStoreFactory(sp))
                .AddActivityTypeProvider<WebhookActivityTypeProvider>()
                .AddBookmarkProvider<WebhookBookmarkProvider>()
                .AddNotificationHandlersFrom<EvictWorkflowRegistryCacheHandler>();

            elsaOptions.ContainerBuilder.Decorate<IWebhookDefinitionStore, InitializingWebhookDefinitionStore>();
            elsaOptions.ContainerBuilder.Decorate<IWebhookDefinitionStore, EventPublishingWebhookDefinitionStore>();

            return elsaOptions;
        }
    }
}