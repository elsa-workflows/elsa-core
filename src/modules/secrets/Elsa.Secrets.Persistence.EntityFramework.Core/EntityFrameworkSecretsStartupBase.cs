using System;
using Elsa.Extensions;
using Elsa.Options;
using Elsa.Persistence.EntityFramework.Core.Options;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Persistence.EntityFramework.Core.Extensions;
using Elsa.Services.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Persistence.EntityFramework.Core
{
    public abstract class EntityFrameworkSecretsStartupBase : StartupBase
    {
        protected abstract string ProviderName { get; }

        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var secretsOptionsBuilder = new SecretsOptionsBuilder(elsa.Services, elsa.ContainerBuilder);

            secretsOptionsBuilder.UseEntityFrameworkPersistence((services, options) 
                => Configure(options, services.GetRequiredService<ElsaDbOptions>()), autoRunMigrations: true);

            elsa.ContainerBuilder.AddScoped(sp => secretsOptionsBuilder.SecretsOptions.SecretsStoreFactory(sp));

            elsa.AddSecrets();
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions);

    }
}
