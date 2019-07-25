using System;
using Elsa.AutoMapper.Extensions;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Persistence.YesSql.Mapping;
using Elsa.Persistence.YesSql.Options;
using Elsa.Persistence.YesSql.Services;
using Elsa.Persistence.YesSql.StartupTasks;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YesSql;
using YesSql.Indexes;

namespace Elsa.Persistence.YesSql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddYesSql(this IServiceCollection services, Action<OptionsBuilder<YesSqlOptions>> configure = null)
        {
            configure?.Invoke(services.AddOptions<YesSqlOptions>());

            return services
                .AddSingleton(StoreFactory.CreateStore)
                .AddSingleton<ISessionProvider, FlushingSessionProvider>()
                .AddSingleton<IIndexProvider, WorkflowDefinitionIndexProvider>()
                .AddSingleton<IIndexProvider, WorkflowInstanceIndexProvider>()
                .AddAutoMapperProfile<DocumentProfile>(ServiceLifetime.Singleton)
                .AddStartupTask<StoreInitializationTask>();
        }

        public static IServiceCollection AddScopedSession(this IServiceCollection services)
        {
            return services
                .AddScoped<ISessionProvider, ManagedSessionProvider>()
                .AddScoped(CreateSession);
        }
        
        public static IServiceCollection AddYesSqlWorkflowInstanceStore(this IServiceCollection services)
        {
            return services
                .AddSingleton<IWorkflowInstanceStore, YesSqlWorkflowInstanceStore>();
        }

        public static IServiceCollection AddYesSqlWorkflowDefinitionStore(this IServiceCollection services)
        {
            return services
                .AddSingleton<IWorkflowDefinitionStore, YesSqlWorkflowDefinitionStore>();
        }

        private static ISession CreateSession(IServiceProvider services)
        {
            var store = services.GetRequiredService<IStore>();
            return store.CreateSession();
        }
    }
}