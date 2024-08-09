using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Features;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Handlers;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Alterations;

/// <summary>
/// Configures the default workflow runtime to use EF Core persistence providers.
/// </summary>
[DependsOn(typeof(AlterationsFeature))]
public class EFCoreAlterationsPersistenceFeature(IModule module) : PersistenceFeatureBase<AlterationsElsaDbContext>(module)
{
    /// Delegate for determining the exception handler.
    public Func<IServiceProvider, IDbExceptionHandler<AlterationsElsaDbContext>> DbExceptionHandler { get; set; } = _ => new NoopDbExceptionHandler();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<AlterationsFeature>(feature =>
        {
            feature.AlterationPlanStoreFactory = sp => sp.GetRequiredService<EFCoreAlterationPlanStore>();
            feature.AlterationJobStoreFactory = sp => sp.GetRequiredService<EFCoreAlterationJobStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        Services.AddScoped(DbExceptionHandler);

        AddEntityStore<AlterationPlan, EFCoreAlterationPlanStore>();
        AddEntityStore<AlterationJob, EFCoreAlterationJobStore>();
    }
}