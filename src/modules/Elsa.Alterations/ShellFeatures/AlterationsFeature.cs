using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Extensions;
using Elsa.Alterations.Core.Stores;
using Elsa.Alterations.Extensions;
using Elsa.Alterations.Services;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.ShellFeatures;

/// <summary>
/// Adds the Elsa alterations services.
/// </summary>
[ShellFeature(
    DisplayName = "Workflow Alterations",
    Description = "Provides capabilities to alter and modify running workflows")]
public class AlterationsFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IAlterationPlanManager, AlterationPlanManager>();
        services.AddAlterations();
        services.AddAlterationsCore();
        services.AddScoped<IAlterationJobDispatcher, BackgroundAlterationJobDispatcher>();
        services.AddScoped<BackgroundAlterationJobDispatcher>();
        services.AddScoped<IAlterationPlanScheduler, DefaultAlterationPlanScheduler>();
        services.AddScoped<IAlterationJobRunner, DefaultAlterationJobRunner>();
        services.AddScoped<IAlterationRunner, DefaultAlterationRunner>();

        services.AddMemoryStore<AlterationPlan, MemoryAlterationPlanStore>();
        services.AddMemoryStore<AlterationJob, MemoryAlterationJobStore>();

        services.AddScoped<IAlterationPlanStore, MemoryAlterationPlanStore>();
        services.AddScoped<IAlterationJobStore, MemoryAlterationJobStore>();
    }
}
