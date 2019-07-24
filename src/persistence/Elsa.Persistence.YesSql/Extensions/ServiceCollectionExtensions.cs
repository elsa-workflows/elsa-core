using System;
using Elsa.Persistence.YesSql.Indexes;
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
        public static IServiceCollection AddYesSql(this IServiceCollection services, Action<OptionsBuilder<YesSqlOptions>> configure)
        {
            configure(services.AddOptions<YesSqlOptions>());

            return services
                .AddSingleton(StoreFactory.CreateStore)
                .AddSingleton<IIndexProvider, WorkflowDefinitionIndexProvider>()
                .AddSingleton<IIndexProvider, WorkflowInstanceIndexProvider>()
                .AddStartupTask<StoreInitializationTask>()
                .AddScoped(CreateSession);
        }
        
        public static IServiceCollection AddYesSqlWorkflowInstanceStore(this IServiceCollection services, Action<OptionsBuilder<YesSqlOptions>> configure)
        {
            return services
                .AddSingleton<IWorkflowInstanceStore, YesSqlWorkflowInstanceStore>();
        }

        public static IServiceCollection AddYesSqlWorkflowDefinitionStore(this IServiceCollection services, Action<OptionsBuilder<YesSqlOptions>> configure)
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