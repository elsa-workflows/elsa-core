using CShells.Features;
using Elsa.Scheduling.Hangfire.Handlers;
using Elsa.Scheduling.Hangfire.Services;
using Elsa.Scheduling.ShellFeatures;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Scheduling.Hangfire.ShellFeatures;

/// <summary>
/// Shell feature that installs Hangfire as <see cref="IWorkflowScheduler"/>.
/// Replaces the default scheduler registration; registering the concrete type alone is not enough.
/// </summary>
[ShellFeature(
    DisplayName = "Hangfire Workflow Scheduler",
    Description = "Uses Hangfire to schedule workflow invocations",
    DependsOn = [typeof(HangfireShellFeature), typeof(SchedulingFeature)])]
[UsedImplicitly]
public class HangfireSchedulerShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<HangfireWorkflowScheduler>();
        services.AddSingleton<IActivityDescriptorModifier, CronActivityDescriptorModifier>();
        services.RemoveAll<IWorkflowScheduler>();
        services.AddSingleton<IWorkflowScheduler>(sp => sp.GetRequiredService<HangfireWorkflowScheduler>());
    }
}
