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
            elsa.UseNonPooledEntityFrameworkPersistence((serviceProvider, options) => Configure(options, serviceProvider), ServiceLifetime.Scoped);
        }

        protected virtual string GetDefaultConnectionString() => throw new Exception($"No connection string specified for the {ProviderName} provider");
        protected abstract void Configure(DbContextOptionsBuilder options, string connectionString);
        protected abstract void Configure(DbContextOptionsBuilder options, IServiceProvider serviceProvider);
    }
}