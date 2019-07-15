using Elsa.Persistence.Memory;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMemoryWorkflowInstanceStore(this IServiceCollection services)
        {
            return services
                .AddSingleton<IWorkflowEventHandler, PersistenceWorkflowEventHandler>()
                .AddSingleton<IWorkflowInstanceStore, MemoryWorkflowInstanceStore>();
        }
        
        public static IServiceCollection AddMemoryWorkflowDefinitionStore(this IServiceCollection services)
        {
            return services.AddSingleton<IWorkflowDefinitionStore, MemoryWorkflowDefinitionStore>();
        }
    }
}