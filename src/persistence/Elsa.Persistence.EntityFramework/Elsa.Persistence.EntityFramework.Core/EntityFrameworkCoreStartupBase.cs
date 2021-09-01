using System;
using Elsa.Options;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Services.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Elsa.Persistence.EntityFramework.Core
{
    public abstract class EntityFrameworkCoreStartupBase : StartupBase
    {
        protected abstract string ProviderName { get; }
        
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
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

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(DbContextOptionsBuilder options, string connectionString);

    }
}