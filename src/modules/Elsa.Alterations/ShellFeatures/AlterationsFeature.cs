using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Extensions;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Stores;
using Elsa.Alterations.Extensions;
using Elsa.Alterations.Services;
using Elsa.Alterations.Workflows;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.ShellFeatures;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.ShellFeatures;

/// <summary>
/// Adds the Elsa alterations services.
/// </summary>
[ShellFeature(
    DisplayName = "Alterations",
    Description = "Provides workflow alteration capabilities for modifying running workflow instances",
    DependsOn = [typeof(ElsaFastEndpointsFeature), typeof(WorkflowRuntimeFeature)])]
[UsedImplicitly]
public class AlterationsFeature : IFastEndpointsShellFeature
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

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<SerializationTypeOptions>(options =>
        {
            options.RegisterTypeAlias(typeof(AlterationPlanParams), nameof(AlterationPlanParams));
            options.RegisterLegacySimpleAssemblyQualifiedName(typeof(AlterationPlanParams));
        });

        services.AddScoped<IAlterationPlanManager, AlterationPlanManager>();
        services.AddAlterations();
        services.AddAlterationsCore();
        services.AddScoped<BackgroundAlterationJobDispatcher>();
        services.AddScoped<IAlterationPlanScheduler, DefaultAlterationPlanScheduler>();
        services.AddScoped<IAlterationJobRunner, DefaultAlterationJobRunner>();
        services.AddScoped<IAlterationRunner, DefaultAlterationRunner>();

        services.AddMemoryStore<AlterationPlan, MemoryAlterationPlanStore>();
        services.AddMemoryStore<AlterationJob, MemoryAlterationJobStore>();

        services.AddScoped(AlterationPlanStoreFactory);
        services.AddScoped(AlterationJobStoreFactory);
        services.AddScoped(AlterationJobDispatcherFactory);

        services.AddWorkflow<ExecuteAlterationPlanWorkflow>();
    }
}
