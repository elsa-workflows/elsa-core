using CShells.Features;
using Elsa.Scheduling.Hangfire.Services;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Scheduling.Hangfire.ShellFeatures;

/// <summary>
/// Shell feature for the Hangfire background activity scheduler. Replaces the default
/// <see cref="IBackgroundActivityScheduler"/> registration; registering the concrete type alone is not enough.
/// </summary>
[ShellFeature(
    DisplayName = "Hangfire Background Activity Scheduler",
    Description = "Uses Hangfire to schedule background activities in workflows",
    DependsOn = [typeof(HangfireShellFeature), typeof(Elsa.Workflows.Runtime.ShellFeatures.WorkflowRuntimeFeature)])]
[UsedImplicitly]
public class HangfireBackgroundActivitySchedulerShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<HangfireBackgroundActivityScheduler>();
        services.RemoveAll<IBackgroundActivityScheduler>();
        services.AddSingleton<IBackgroundActivityScheduler>(sp => sp.GetRequiredService<HangfireBackgroundActivityScheduler>());
    }
}
