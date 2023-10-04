using Elsa.Alterations.BackgroundRunner.Features;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Extensions;
using Elsa.Alterations.Core.Stores;
using Elsa.Alterations.Extensions;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Features;

/// <summary>
/// Adds the Elsa alterations services.
/// </summary>
[DependsOn(typeof(AlterationsBackgroundRunnerFeature))]
public class AlterationsFeature : FeatureBase
{
    /// <inheritdoc />
    public AlterationsFeature(IModule module) : base(module)
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

    /// <summary>
    /// Adds an alteration and its handler.
    /// </summary>
    /// <typeparam name="T">The type of alteration.</typeparam>
    /// <typeparam name="THandler">The type of alteration handler.</typeparam>
    public AlterationsFeature AddAlteration<T, THandler>() where T : class, IAlteration where THandler : class, IAlterationHandler
    {
        Services.AddAlteration<T, THandler>();
        return this;
    }
    
    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<AlterationsFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddAlterations();
        Services.AddAlterationsCore();
        Services.AddSingleton<MemoryAlterationPlanStore>();
        Services.AddSingleton<MemoryAlterationJobStore>();
        Services.AddSingleton(new MemoryStore<AlterationPlan>());
        Services.AddSingleton(new MemoryStore<AlterationJob>());
        Services.AddSingleton(AlterationPlanStoreFactory);
        Services.AddSingleton(AlterationJobStoreFactory);
    }
}