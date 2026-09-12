using Elsa.Persistence.VNext.Contracts;
using Elsa.UserTasks.Persistence.VNext.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.UserTasks.Persistence.VNext.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserTasksPersistenceVNext(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPersistenceSchemaProvider, UserTaskPersistenceSchemaProvider>());
        services.Replace(ServiceDescriptor.Scoped<Elsa.UserTasks.Contracts.IUserTaskRepository, VNextUserTaskRepository>());
        return services;
    }
}
