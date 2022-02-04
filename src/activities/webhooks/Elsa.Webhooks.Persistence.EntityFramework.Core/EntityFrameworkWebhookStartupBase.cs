using System;
using Elsa.Activities.Webhooks;
using Elsa.Options;
using Elsa.Services.Startup;
using Elsa.Webhooks.Persistence.EntityFramework.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core
{
    public abstract class EntityFrameworkWebhookStartupBase : StartupBase
    {
        protected abstract string ProviderName { get; }

        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var optionsBuilder = new WebhookOptionsBuilder(elsa.Services);

            optionsBuilder.UseNonPooledEntityFrameworkPersistence((serviceProvider, options) => ConfigureFor(options, serviceProvider), ServiceLifetime.Scoped);

            elsa.Services.AddSingleton(optionsBuilder.WebhookOptions);
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(DbContextOptionsBuilder options, string connectionString);
        protected abstract void ConfigureFor(DbContextOptionsBuilder options, IServiceProvider serviceProvider);
    }
}