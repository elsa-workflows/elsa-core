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
        public static ElsaOptions AddYesSqlStores(
            this ElsaOptions options,
            Action<IConfiguration> configure)
        {
            return options
                .UseYesSqlWorkflowDefinitionStore(configure)
                .UseYesSqlWorkflowDefinitionVersionStore(configure)
                .UseYesSqlWorkflowInstanceStore(configure);
        }

        public static ElsaOptions UseYesSqlWorkflowInstanceStore(
            this ElsaOptions options,
            Action<IConfiguration> configure)
        {
            options
                .AddYesSqlProvider(configure)
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<YesSqlWorkflowInstanceStore>())
                .Services
                .AddScoped<IWorkflowInstanceStore, YesSqlWorkflowInstanceStore>();

            return options;
        }

        public static ElsaOptions UseYesSqlWorkflowDefinitionVersionStore(
            this ElsaOptions options,
            Action<IConfiguration> configure)
        {
            options
                .AddYesSqlProvider(configure)
                .UseWorkflowDefinitionVersionStore(sp => sp.GetRequiredService<YesSqlWorkflowDefinitionVersionStore>())
                .Services
                .AddScoped<YesSqlWorkflowDefinitionVersionStore>();

            return options;
        }
        public static ElsaOptions UseYesSqlWorkflowDefinitionStore(
            this ElsaOptions options,
            Action<IConfiguration> configure)
        {
            options
                .AddYesSqlProvider(configure)
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<YesSqlWorkflowDefinitionStore>())
                .Services
                .AddScoped<YesSqlWorkflowDefinitionStore>();

            return options;
        }

        private static ElsaOptions AddYesSqlProvider(
            this ElsaOptions options,
            Action<IConfiguration> configure)
        {
            if (options.Services.HasService<IStore>())
                return options;

            options.Services
                .AddSingleton(sp => StoreFactory.CreateStore(sp, configure))
                .AddSingleton<IIndexProvider, WorkflowDefinitionVersionIndexProvider>()
                .AddSingleton<IIndexProvider, WorkflowInstanceIndexProvider>()
                .AddTransient<ISchemaVersionStore, SchemaVersionStore>()
                .AddScoped(CreateSession)
                .AddAutoMapperProfile<NodaTimeProfile>(ServiceLifetime.Singleton)
                .AddAutoMapperProfile<DocumentProfile>(ServiceLifetime.Singleton)
                .AddStartupTask<InitializeStoreTask>();

            return new ElsaOptions(options.Services);
        }

        private static ISession CreateSession(IServiceProvider services)
        {
            var store = services.GetRequiredService<IStore>();
            return store.CreateSession();
        }
    }
}