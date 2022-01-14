using System;
using Elsa.Options;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Services.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFramework.Core
{
    public abstract class EntityFrameworkCoreStartupBase : StartupBase
    {
        protected abstract string ProviderName { get; }

        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var multiTenancyEnabled = configuration.GetValue<bool>("Elsa:MultiTenancy");

            if (multiTenancyEnabled)
                elsa.UseNonPooledEntityFrameworkPersistence((serviceProvider, options) => ConfigureForMultitenancy(options, serviceProvider), ServiceLifetime.Scoped, autoRunMigrations: false, multitenancyEnabled: true);
            else
            {
                var section = configuration.GetSection($"Elsa:Features:DefaultPersistence");
                var connectionStringName = section.GetValue<string>("ConnectionStringIdentifier");
                var connectionString = section.GetValue<string>("ConnectionString");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    if (string.IsNullOrWhiteSpace(connectionStringName))
                        connectionStringName = ProviderName;

                    connectionString = configuration.GetConnectionString(connectionStringName);
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                    connectionString = GetDefaultConnectionString();

                elsa.UseEntityFrameworkPersistence(options => Configure(options, connectionString));
            }
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(DbContextOptionsBuilder options, string connectionString);
        protected abstract void ConfigureForMultitenancy(DbContextOptionsBuilder options, IServiceProvider serviceProvider);
    }
}