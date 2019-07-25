using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Memory
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMemoryWorkflowInstanceStore(this IServiceCollection services)
        {
            return services
                .AddSingleton<IWorkflowInstanceStore, MemoryWorkflowInstanceStore>();
        }
        
        public static IServiceCollection AddMemoryWorkflowDefinitionStore(this IServiceCollection services)
        {
            return services.AddSingleton<IWorkflowDefinitionStore, MemoryWorkflowDefinitionStore>();
        }
    }
}