using System;
using Elsa.AutoMapper.Extensions;
using Elsa.AutoMapper.Extensions.NodaTime;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Mapping;
using Elsa.Persistence.YesSql.Services;
using Elsa.Persistence.YesSql.StartupTasks;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static YesSqlServiceConfiguration WithYesSqlProvider(
            this ServiceConfiguration configuration,
            Action<IConfiguration> configure)
        {
            configuration.Services
                .AddSingleton(sp => StoreFactory.CreateStore(sp, configure))
                .AddSingleton<IIndexProvider, WorkflowDefinitionIndexProvider>()
                .AddSingleton<IIndexProvider, WorkflowInstanceIndexProvider>()
                .AddScoped(CreateSession)
                .AddAutoMapperProfile<InstantProfile>(ServiceLifetime.Singleton)
                .AddAutoMapperProfile<DocumentProfile>(ServiceLifetime.Singleton)
                .AddStartupTask<InitializeStoreTask>();

            return new YesSqlServiceConfiguration(configuration.Services);
        }

        public static YesSqlServiceConfiguration WithYesSqlStores(
            this ServiceConfiguration configuration,
            Action<IConfiguration> configure)
        {
            return configuration.WithYesSqlProvider(configure).WithWorkflowDefinitionStore()
                .WithWorkflowInstanceStore();
        }

        public static YesSqlServiceConfiguration WithWorkflowInstanceStore(
            this YesSqlServiceConfiguration configuration)
        {
            configuration.Services.AddScoped<IWorkflowInstanceStore, YesSqlWorkflowInstanceStore>();
            return configuration;
        }

        public static YesSqlServiceConfiguration WithWorkflowDefinitionStore(
            this YesSqlServiceConfiguration configuration)
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