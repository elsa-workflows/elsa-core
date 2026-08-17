using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EFCore;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Features;
using Elsa.UserTasks.Persistence.EFCore.Contracts;
using Elsa.UserTasks.Persistence.EFCore.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.Persistence.EFCore.Features;

/// <summary>
/// Configures the identity-neutral User Tasks repository with EF Core persistence.
/// </summary>
[DependsOn(typeof(UserTasksFeature))]
public class EFCoreUserTasksPersistenceFeature(IModule module)
    : PersistenceFeatureBase<EFCoreUserTasksPersistenceFeature, UserTasksElsaDbContext>(module)
{
    public override void Apply()
    {
        base.Apply();
        AddStore<UserTaskRecord, EFCoreUserTaskRepository>();
        Services.AddScoped<IUserTaskRepository, EFCoreUserTaskRepository>();
        Services.AddScoped<IUserTaskPersistenceAdapter, EFCoreUserTaskRepository>();
    }
}
