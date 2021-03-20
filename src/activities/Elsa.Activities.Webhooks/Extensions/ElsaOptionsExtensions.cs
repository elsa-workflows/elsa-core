using System;
using Elsa;
using Elsa.Activities.Webhooks.ActivityTypes;
using Elsa.Activities.Webhooks.Options;
using Elsa.Activities.Webhooks.Persistence;
using Elsa.Activities.Webhooks.Persistence.Decorators;
using Elsa.ActivityProviders;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions AddWebhooks(
            this ElsaOptions elsaOptions,
            Action<WebhookOptions>? configure = default)
        {
            var services = elsaOptions.Services;

            // Configure Webhooks.
            var options = new WebhookOptions();
            configure?.Invoke(options);

            // Services.
            services
                .AddScoped<IActivityTypeProvider, WebhookActivityTypeProvider>()
                .AddScoped(options.WebhookDefinitionStoreFactory);

            services.Decorate<IWebhookDefinitionStore, InitializingWebhookDefinitionStore>();

            return elsaOptions;
        }
    }
}
