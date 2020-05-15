using System;
using Elsa.Extensions;
using Elsa.Mapping;
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
        public static YesSqlElsaBuilder AddYesSqlProvider(
            this ElsaBuilder configuration,
            Action<IConfiguration> configure)
        {
            configuration.Services
                .AddSingleton(sp => StoreFactory.CreateStore(sp, configure))
                .AddSingleton<IIndexProvider, WorkflowDefinitionIndexProvider>()
                .AddSingleton<IIndexProvider, WorkflowInstanceIndexProvider>()
                .AddTransient<ISchemaVersionStore, SchemaVersionStore>()
                .AddScoped(CreateSession)
                .AddMapperProfile<NodaTimeProfile>(ServiceLifetime.Singleton)
                .AddMapperProfile<DocumentProfile>(ServiceLifetime.Singleton)
                .AddStartupTask<InitializeStoreTask>();

            return new YesSqlElsaBuilder(configuration.Services);
        }

        public static YesSqlElsaBuilder AddYesSqlStores(
            this ElsaBuilder configuration,
            Action<IConfiguration> configure)
        {
            return configuration.AddYesSqlProvider(configure).AddWorkflowDefinitionStore()
                .AddWorkflowInstanceStore();
        }

        public static YesSqlElsaBuilder AddWorkflowInstanceStore(
            this YesSqlElsaBuilder configuration)
        {
            configuration.Services.AddScoped<IWorkflowInstanceStore, YesSqlWorkflowInstanceStore>();
            return configuration;
        }

        public static YesSqlElsaBuilder AddWorkflowDefinitionStore(
            this YesSqlElsaBuilder configuration)
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