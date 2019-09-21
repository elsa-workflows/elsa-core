using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Memory
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMemoryWorkflowInstanceStore(this IServiceCollection services)
        {
            if (services.All(x => x.ServiceType != typeof(IWorkflowInstanceStore)))
                services.AddSingleton<IWorkflowInstanceStore, MemoryWorkflowInstanceStore>();

            return services;
        }

        public static IServiceCollection AddMemoryWorkflowDefinitionStore(this IServiceCollection services)
        {
            if (services.All(x => x.ServiceType != typeof(IWorkflowDefinitionStore)))
                services.AddSingleton<IWorkflowDefinitionStore, MemoryWorkflowDefinitionStore>();

            return services;
        }
    }
}