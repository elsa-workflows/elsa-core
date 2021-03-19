using System;
using Elsa.Activities.Webhooks;
using Elsa.Activities.Webhooks.Persistence;
using Elsa.Activities.Webhooks.Persistence.Decorators;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebhooks(
            this IServiceCollection services,
            Action<WebhookOptions>? configure = default)
        {
            var options = new WebhookOptions();
            configure?.Invoke(options);

            services
                .AddSingleton(options)
                .AddScoped(options.WebhookDefinitionStoreFactory);

            services.Decorate<IWebhookDefinitionStore, InitializingWebhookDefinitionStore>();

            return services;
        }
    }
}
