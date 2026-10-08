using CShells.Features;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Scheduling.Quartz.ShellFeatures;

/// <summary>
/// Shell feature for the Quartz.NET background activity scheduler. Replaces the default
/// <see cref="IBackgroundActivityScheduler"/> registration; registering the concrete type alone is not enough.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz Background Activity Scheduler",
    Description = "Uses Quartz.NET to schedule background activities in workflows",
    DependsOn = [typeof(QuartzFeature), typeof(Elsa.Workflows.Runtime.ShellFeatures.WorkflowRuntimeFeature)])]
[UsedImplicitly]
public class QuartzBackgroundActivitySchedulerShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<QuartzBackgroundActivityScheduler>();
        services.RemoveAll<IBackgroundActivityScheduler>();
        services.AddSingleton<IBackgroundActivityScheduler>(sp => sp.GetRequiredService<QuartzBackgroundActivityScheduler>());
    }
}
