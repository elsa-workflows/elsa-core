using CShells.Features;
using Elsa.Alterations.Core.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Alterations;

/// <summary>
/// Configures the alterations feature with an Entity Framework Core persistence provider.
/// </summary>
[ShellFeature(
    DisplayName = "EF Core Alterations Persistence",
    Description = "Provides Entity Framework Core persistence for workflow alterations",
    DependsOn = ["Alterations"])]
[UsedImplicitly]
public class EFCoreAlterationsPersistenceShellFeature : PersistenceShellFeatureBase<EFCoreAlterationsPersistenceShellFeature, AlterationsElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        AddEntityStore<AlterationPlan, EFCoreAlterationPlanStore>(services);
        AddEntityStore<AlterationJob, EFCoreAlterationJobStore>(services);
    }
}
