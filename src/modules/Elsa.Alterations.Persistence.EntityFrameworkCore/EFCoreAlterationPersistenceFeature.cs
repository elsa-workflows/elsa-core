using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Features;
using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Persistence.EntityFrameworkCore;

/// <summary>
/// Configures the default workflow runtime to use EF Core persistence providers.
/// </summary>
[DependsOn(typeof(CoreAlterationsFeature))]
public class EFCoreAlterationPersistenceFeature : PersistenceFeatureBase<AlterationsDbContext>
{
    /// <inheritdoc />
    public EFCoreAlterationPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<CoreAlterationsFeature>(feature =>
        {
            feature.AlterationPlanStoreFactory = sp => sp.GetRequiredService<EFCoreAlterationPlanStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddEntityStore<AlterationPlan, EFCoreAlterationPlanStore>();
    }
}