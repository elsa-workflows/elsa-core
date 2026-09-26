using CShells.Features;
using Elsa.Extensions;
using Elsa.Resilience.ShellFeatures;
using Elsa.Scheduling.Quartz.Handlers;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Scheduling.Quartz.Tasks;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.ShellFeatures;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.ShellFeatures;

/// <summary>
/// A feature that installs Quartz.NET implementations for <see cref="IWorkflowScheduler"/>.
/// </summary>
[ShellFeature(
    DisplayName = "Quartz Workflow Scheduler",
    Description = "Uses Quartz.NET to schedule workflow execution.",
    DependsOn = [
    typeof(QuartzFeature), 
    typeof(SchedulingFeature), 
    typeof(ResilienceFeature)])]
[UsedImplicitly]
public class QuartzSchedulerFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IActivityDescriptorModifier, CronActivityDescriptorModifier>()
            .AddSingleton<IQuartzScheduleCoordinator, QuartzScheduleCoordinator>()
            .AddScoped<IJobKeyProvider, JobKeyProvider>()
            .AddSingleton<IQuartzRetryDelayCalculator, QuartzRetryDelayCalculator>()
            .AddSingleton<IQuartzJobRetryScheduler, QuartzJobRetryScheduler>()
            .AddStartupTask<RegisterJobsTask>()
            .AddScoped<IWorkflowScheduler, QuartzWorkflowScheduler>()
            .AddSingleton<ICronParser, QuartzCronParser>()
            .AddQuartz();
    }
}
