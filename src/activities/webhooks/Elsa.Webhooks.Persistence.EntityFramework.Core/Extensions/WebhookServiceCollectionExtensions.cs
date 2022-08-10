using System;
using Autofac;
using Autofac.Multitenant;
using Elsa.Activities.Webhooks;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Multitenancy.Extensions;
using Elsa.Persistence.EntityFramework.Core.Options;
using Elsa.Runtime;
using Elsa.Webhooks.Persistence.EntityFramework.Core.Services;
using Elsa.Webhooks.Persistence.EntityFramework.Core.StartupTasks;
using Elsa.Webhooks.Persistence.EntityFramework.Core.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core.Extensions
{
    public static class WebhookServiceCollectionExtensions
    {
        public static WebhookOptionsBuilder UseEntityFrameworkPersistence(this WebhookOptionsBuilder webhookOptions,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            bool autoRunMigrations = false) =>
            UseEntityFrameworkPersistence<WebhookContext>(webhookOptions, configure, autoRunMigrations);

        static WebhookOptionsBuilder UseEntityFrameworkPersistence<TWebhookContext>(WebhookOptionsBuilder webhookOptions,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            bool autoRunMigrations) where TWebhookContext : WebhookContext
        {
            webhookOptions.Services.AddDbContextFactory<TWebhookContext>(configure, ServiceLifetime.Scoped);

            webhookOptions.ContainerBuilder
                .Register(cc =>
                {
                    var tenant = cc.Resolve<ITenant>();
                    return new ElsaDbOptions(tenant!.GetDatabaseConnectionString()!);
                }).IfNotRegistered(typeof(ElsaDbOptions)).InstancePerTenant();

            webhookOptions.ContainerBuilder
                .AddMultiton<IWebhookContextFactory, WebhookContextFactory<TWebhookContext>>()
                .AddScoped<EntityFrameworkWebhookDefinitionStore>();

            if (autoRunMigrations)
                webhookOptions.ContainerBuilder.AddStartupTask<RunMigrations>();

            webhookOptions.UseWebhookDefinitionStore(sp => sp.GetRequiredService<EntityFrameworkWebhookDefinitionStore>());

            return webhookOptions;
        }
    }
}
