using System;
using Autofac;
using Autofac.Multitenant;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Multitenancy.Extensions;
using Elsa.Persistence.EntityFramework.Core.Options;
using Elsa.Runtime;
using Elsa.Secrets.Persistence.EntityFramework.Core.Services;
using Elsa.Secrets.Persistence.EntityFramework.Core.StartupTasks;
using Elsa.Secrets.Persistence.EntityFramework.Core.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Persistence.EntityFramework.Core.Extensions
{
    public static class SecretsServiceCollectionExtensions
    {
        public static SecretsOptionsBuilder UseEntityFrameworkPersistence(this SecretsOptionsBuilder secretsOptions,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            bool autoRunMigrations = false) =>
            UseEntityFrameworkPersistence<SecretsContext>(secretsOptions, configure, autoRunMigrations);

        static SecretsOptionsBuilder UseEntityFrameworkPersistence<TSecretsContext>(SecretsOptionsBuilder secretsOptions,
            Action<IServiceProvider, DbContextOptionsBuilder> configure,
            bool autoRunMigrations) where TSecretsContext : SecretsContext
        {
            secretsOptions.Services.AddDbContextFactory<TSecretsContext>(configure, ServiceLifetime.Scoped);

            secretsOptions.ContainerBuilder
                .Register(cc =>
                {
                    var tenant = cc.Resolve<ITenant>();
                    return new ElsaDbOptions(tenant!.GetDatabaseConnectionString()!);
                }).IfNotRegistered(typeof(ElsaDbOptions)).InstancePerTenant();

            secretsOptions.ContainerBuilder
                .AddMultiton<ISecretsContextFactory, SecretsContextFactory<TSecretsContext>>()
                .AddScoped<EntityFrameworkSecretsStore>();

            if (autoRunMigrations)
                secretsOptions.ContainerBuilder.AddStartupTask<RunMigrations>();

            secretsOptions.UseSecretsStore(sp => sp.GetRequiredService<EntityFrameworkSecretsStore>());

            return secretsOptions;
        }
    }
}
