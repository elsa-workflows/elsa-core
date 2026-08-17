using Elsa.Persistence.EFCore;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Persistence.EFCore.Contracts;
using Elsa.UserTasks.Persistence.EFCore.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.Persistence.EFCore.ShellFeatures;

/// <summary>
/// Shared provider shell base for User Tasks persistence.
/// </summary>
public abstract class EFCoreUserTasksPersistenceShellFeatureBase : PersistenceShellFeatureBase<UserTasksElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        AddStore<UserTaskRecord, EFCoreUserTaskRepository>(services);
        services.AddScoped<IUserTaskRepository, EFCoreUserTaskRepository>();
        services.AddScoped<IUserTaskPersistenceAdapter, EFCoreUserTaskRepository>();
    }
}
