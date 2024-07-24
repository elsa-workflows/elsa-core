using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Extensions;
using Elsa.Alterations.Core.Stores;
using Elsa.Alterations.Extensions;
using Elsa.Alterations.Services;
using Elsa.Alterations.Workflows;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Features;

/// <summary>
/// Adds the Elsa alterations services.
/// </summary>
/// <inheritdoc />
public class AlterationsFeature(IModule module) : FeatureBase(module)
{

    /// <summary>
    /// Gets or sets the factory for the alteration plan store.
    /// </summary>
    public Func<IServiceProvider, IAlterationPlanStore> AlterationPlanStoreFactory { get; set; } = sp => sp.GetRequiredService<MemoryAlterationPlanStore>();

    /// <summary>
    /// Gets or sets the factory for the alteration job store.
    /// </summary>
    public Func<IServiceProvider, IAlterationJobStore> AlterationJobStoreFactory { get; set; } = sp => sp.GetRequiredService<MemoryAlterationJobStore>();

    /// <summary>
    /// Gets or sets the factory for the alteration job dispatcher.
    /// </summary>
    public Func<IServiceProvider, IAlterationJobDispatcher> AlterationJobDispatcherFactory { get; set; } = sp => sp.GetRequiredService<BackgroundAlterationJobDispatcher>();

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
        Module.AddWorkflow<ExecuteAlterationPlanWorkflow>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped<IAlterationPlanManager, AlterationPlanManager>();
        Services.AddAlterations();
        Services.AddAlterationsCore();
        Services.AddScoped<BackgroundAlterationJobDispatcher>();
        Services.AddScoped<IAlterationPlanScheduler, DefaultAlterationPlanScheduler>();
        Services.AddScoped<IAlterationJobRunner, DefaultAlterationJobRunner>();
        Services.AddScoped<IAlterationRunner, DefaultAlterationRunner>();

        Services.AddMemoryStore<AlterationPlan, MemoryAlterationPlanStore>();
        Services.AddMemoryStore<AlterationJob, MemoryAlterationJobStore>();

        Services.AddScoped(AlterationPlanStoreFactory);
        Services.AddScoped(AlterationJobStoreFactory);
        Services.AddScoped(AlterationJobDispatcherFactory);
    }
}