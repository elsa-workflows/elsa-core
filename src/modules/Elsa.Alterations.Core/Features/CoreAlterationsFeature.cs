using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Extensions;
using Elsa.Alterations.Core.Stores;
using Elsa.Common.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Core.Features;

/// <summary>
/// Adds the core Elsa alterations services.
/// </summary>
public class CoreAlterationsFeature : FeatureBase
{
    /// <inheritdoc />
    public CoreAlterationsFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// Gets or sets the factory for the alteration plan store.
    /// </summary>
    public Func<IServiceProvider, IAlterationPlanStore> AlterationPlanStoreFactory { get; set; } = sp => sp.GetRequiredService<MemoryAlterationPlanStore>();
    
    /// <summary>
    /// Gets or sets the factory for the alteration job store.
    /// </summary>
    public Func<IServiceProvider, IAlterationJobStore> AlterationJobStoreFactory { get; set; } = sp => sp.GetRequiredService<MemoryAlterationJobStore>();

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddAlterationsCore();
        Services.AddSingleton<MemoryAlterationPlanStore>();
        Services.AddSingleton<MemoryAlterationJobStore>();
        Services.AddSingleton(new MemoryStore<AlterationPlan>());
        Services.AddSingleton(new MemoryStore<AlterationJob>());
        Services.AddSingleton(AlterationPlanStoreFactory);
        Services.AddSingleton(AlterationJobStoreFactory);
    }
}