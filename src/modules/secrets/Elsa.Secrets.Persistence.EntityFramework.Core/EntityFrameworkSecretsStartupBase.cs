using Elsa.Options;
using Elsa.Secrets.Extentions;
using Elsa.Secrets.Persistence.EntityFramework.Core.Extentions;
using Elsa.Services.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Persistence.EntityFramework.Core
{
    public abstract class EntityFrameworkSecretsStartupBase : StartupBase
    {
        protected abstract string ProviderName { get; }

        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var services = elsa.Services;
            var section = configuration.GetSection($"Elsa:Features:Secrets");
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

            var secretsOptionsBuilder = new SecretsOptionsBuilder(services);
            secretsOptionsBuilder.UseEntityFrameworkPersistence(options => Configure(options, connectionString));
            services.AddScoped(sp => secretsOptionsBuilder.SectersOptions.SecretsStoreFactory(sp));

            elsa.AddSecrets();
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(DbContextOptionsBuilder options, string connectionString);

    }
}
