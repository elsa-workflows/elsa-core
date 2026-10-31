using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Alterations;

/// <summary>
/// Base class for alterations persistence features.
/// This is not a standalone shell feature - use provider-specific features.
/// </summary>
[UsedImplicitly]
public abstract class EFCoreAlterationsPersistenceShellFeatureBase : PersistenceShellFeatureBase<AlterationsElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddScoped<IAlterationPlanStore, EFCoreAlterationPlanStore>();
        services.AddScoped<IAlterationJobStore, EFCoreAlterationJobStore>();
        
        AddEntityStore<AlterationPlan, EFCoreAlterationPlanStore>(services);
        AddEntityStore<AlterationJob, EFCoreAlterationJobStore>(services);
    }
}
