using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Resilience.Features;
using Elsa.Scheduling.Quartz.Handlers;
using Elsa.Scheduling.Quartz.Options;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Scheduling.Quartz.Tasks;
using Elsa.Scheduling.Features;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.Features;

/// <summary>
/// A feature that installs Quartz.NET implementations for <see cref="IWorkflowScheduler"/>.
/// </summary>
[DependsOn(typeof(SchedulingFeature))]
[DependsOn(typeof(ResilienceFeature))]
[UsedImplicitly]
public class QuartzSchedulerFeature(IModule module) : FeatureBase(module)
{
    private Action<QuartzJobOptions> _configureQuartzJobOptions = _ => { };

    [PublicAPI]
    public QuartzSchedulerFeature ConfigureOptions(Action<QuartzJobOptions> configure)
    {
        _configureQuartzJobOptions += configure;
        return this;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<SchedulingFeature>(scheduling =>
        {
            // Configure the scheduling feature to use the Quartz workflow scheduler.
            scheduling.WorkflowScheduler = sp => sp.GetRequiredService<QuartzWorkflowScheduler>();

            // Configure the cron parser to use the Quartz cron parser.
            scheduling.CronParser = sp => sp.GetRequiredService<QuartzCronParser>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services
            .AddSingleton<IActivityDescriptorModifier, CronActivityDescriptorModifier>()
            .AddSingleton<QuartzCronParser>()
            .AddScoped<QuartzWorkflowScheduler>()
            .AddSingleton<IQuartzScheduleCoordinator, QuartzScheduleCoordinator>()
            .AddScoped<IJobKeyProvider, JobKeyProvider>()
            .AddSingleton<IQuartzRetryDelayCalculator, QuartzRetryDelayCalculator>()
            .AddSingleton<IQuartzJobRetryScheduler, QuartzJobRetryScheduler>()
            .AddStartupTask<RegisterJobsTask>()
            .AddQuartz();

        Services.Configure(_configureQuartzJobOptions);
    }
}
