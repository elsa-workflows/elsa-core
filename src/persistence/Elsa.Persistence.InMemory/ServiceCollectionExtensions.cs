using Elsa.WorkflowProviders;

using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.InMemory
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaPersistenceInMemory(this IServiceCollection services)
        {
            // Scoped, because IMediator is registered as Scoped and IIdGenerator could also be registered as Scoped
            services.AddScoped<IWorkflowDefinitionStore, InMemoryWorkflowDefinitionStore>()
                .AddScoped<IWorkflowInstanceStore, InMemoryWorkflowInstanceStore>()
                .AddWorkflowProvider<DatabaseWorkflowProvider>();

            return services;
        }
    }
}
