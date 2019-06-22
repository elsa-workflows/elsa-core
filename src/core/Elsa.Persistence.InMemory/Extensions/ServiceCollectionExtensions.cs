using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Persistence.InMemory.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static IServiceCollection AddInMemoryServices(this IServiceCollection services)
        {
            services.TryAddSingleton<IWorkflowEventHandler, PersistenceWorkflowEventHandler>();
            services.TryAddTransient<IInMemoryWorkflowProvider, InMemoryWorkflowProvider>();
            return services;
        }
    }
}