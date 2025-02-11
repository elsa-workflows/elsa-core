using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Quartz.Contracts;
using Elsa.Quartz.Handlers;
using Elsa.Quartz.Services;
using Elsa.Quartz.Tasks;
using Elsa.Scheduling;
using Elsa.Scheduling.Features;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Quartz.Features;

/// <summary>
/// A feature that installs Quartz.NET implementations for <see cref="IWorkflowScheduler"/>.
/// </summary>
[DependsOn(typeof(SchedulingFeature))]
public class QuartzSchedulerFeature(IModule module) : FeatureBase(module)
{
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
            .AddScoped<IJobKeyProvider, JobKeyProvider>()
            .AddStartupTask<RegisterJobsTask>()
            .AddQuartz();
    }
}