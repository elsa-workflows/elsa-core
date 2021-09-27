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
            var section = configuration.GetSection($"Elsa:Features:Webhooks");
            var connectionStringName = section.GetValue<string>("ConnectionStringIdentifier");
            var connectionString = section.GetValue<string>("ConnectionString");

            if (string.IsNullOrWhiteSpace(connectionStringName))
                connectionStringName = ProviderName;

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = configuration.GetConnectionString(connectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = GetDefaultConnectionString();

            var optionsBuilder = new WebhookOptionsBuilder(elsa.Services);
            optionsBuilder.UseEntityFrameworkPersistence(ef => Configure(ef, connectionString));
            elsa.Services.AddSingleton(optionsBuilder.WebhookOptions);
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(DbContextOptionsBuilder options, string connectionString);
    }
}