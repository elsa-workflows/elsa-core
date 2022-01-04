using Elsa.Persistence.Mappers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services)
    {
        return services.AddSingleton<WorkflowDefinitionMapper>();
    }
}