using System;
using Elsa.AutoMapper.Extensions;
using Elsa.AutoMapper.Extensions.NodaTime;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Mapping;
using Elsa.Persistence.YesSql.Schema;
using Elsa.Persistence.YesSql.Services;
using Elsa.Persistence.YesSql.StartupTasks;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Extensions
{
    public static class YesSqlServiceCollectionExtensions
    {
        public static YesSqlElsaOptions AddYesSqlProvider(
            this ElsaOptions configuration,
            Action<IConfiguration> configure)
        {
            configuration.Services
                .AddSingleton(sp => StoreFactory.CreateStore(sp, configure))
                .AddSingleton<IIndexProvider, WorkflowDefinitionIndexProvider>()
                .AddSingleton<IIndexProvider, WorkflowInstanceIndexProvider>()
                .AddTransient<ISchemaVersionStore, SchemaVersionStore>()
                .AddScoped(CreateSession)
                .AddAutoMapperProfile<NodaTimeProfile>(ServiceLifetime.Singleton)
                .AddAutoMapperProfile<DocumentProfile>(ServiceLifetime.Singleton)
                .AddStartupTask<InitializeStoreTask>();

            return new YesSqlElsaOptions(configuration.Services);
        }

        public static YesSqlElsaOptions AddYesSqlStores(
            this ElsaOptions configuration,
            Action<IConfiguration> configure)
        {
            return configuration.AddYesSqlProvider(configure).AddWorkflowDefinitionStore()
                .AddWorkflowInstanceStore();
        }

        public static YesSqlElsaOptions AddWorkflowInstanceStore(
            this YesSqlElsaOptions configuration)
        {
            configuration.Services.AddScoped<IWorkflowInstanceStore, YesSqlWorkflowInstanceStore>();
            return configuration;
        }

        public static YesSqlElsaOptions AddWorkflowDefinitionStore(
            this YesSqlElsaOptions configuration)
        {
            configuration.Services
                .AddScoped<IWorkflowDefinitionStore, YesSqlWorkflowDefinitionStore>();

            return configuration;
        }

        private static ISession CreateSession(IServiceProvider services)
        {
            var store = services.GetRequiredService<IStore>();
            return store.CreateSession();
        }
    }
}