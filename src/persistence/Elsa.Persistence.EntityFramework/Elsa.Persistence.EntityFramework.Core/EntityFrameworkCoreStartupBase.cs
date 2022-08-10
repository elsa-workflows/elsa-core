using System;
using Elsa.Options;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Core.Options;
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
            elsa.UseEntityFrameworkPersistenceForMultitenancy((services, options) 
                => Configure(options, services.GetRequiredService<ElsaDbOptions>()), autoRunMigrations: true);
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");

        protected abstract void Configure(DbContextOptionsBuilder options, ElsaDbOptions elsaDbOptions);
    }
}