using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Persistence.InMemory.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemoryWorkflowDefinitionStoreProvider(this IServiceCollection services)
        {
            services.AddInMemoryServices();
            services.TryAddSingleton<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>();

            return services;
        }

        public static IServiceCollection AddInMemoryWorkflowInstanceStoreProvider(this IServiceCollection services)
        {
            services.AddInMemoryServices();
            services.TryAddSingleton<IWorkflowInstanceStore, InMemoryWorkflowInstanceStore>();

            return services;
        }

        private static IServiceCollection AddInMemoryServices(this IServiceCollection services)
        {
            services.TryAddSingleton<IWorkflowEventHandler, PersistenceWorkflowEventHandler>();
            services.TryAddTransient<IInMemoryWorkflowProvider, InMemoryWorkflowProvider>();
            return services;
        }
    }
}