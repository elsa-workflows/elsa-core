using Elsa.Repositories;
using Elsa.WorkflowProviders;

using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.InMemory
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaPersistenceInMemory(this IServiceCollection services)
        {
            // Scoped, because IMediator is registered as Scoped and IIdGenerator could also be registered as Scoped
            services.AddScoped<IWorkflowDefinitionRepository, InMemoryWorkflowDefinitionRepository>()
                .AddScoped<IWorkflowInstanceStore, InMemoryWorkflowInstanceStore>()
                .AddWorkflowProvider<DatabaseWorkflowProvider>();

            return services;
        }
    }
}
