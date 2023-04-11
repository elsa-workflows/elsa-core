using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Quartz.Jobs;
using Elsa.Quartz.Services;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Features;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Quartz.Features;

/// <summary>
/// A feature that installs Quartz.NET implementations for <see cref="IWorkflowScheduler"/>.
/// </summary>
[DependsOn(typeof(SchedulingFeature))]
public class QuartzSchedulerFeature : FeatureBase
{
    /// <inheritdoc />
    public QuartzSchedulerFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate that creates a <see cref="QuartzWorkflowScheduler"/> instance.
    /// </summary>
    public Func<IServiceProvider, IWorkflowScheduler> WorkflowScheduler { get; set; } = sp => sp.GetRequiredService<QuartzWorkflowScheduler>();

    /// <inheritdoc />
    public override void Configure()
    {
        // Configure the scheduling feature to use the Quartz workflow scheduler.
        Module.Configure<SchedulingFeature>(scheduling => scheduling.WorkflowScheduler = WorkflowScheduler);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<QuartzWorkflowScheduler>();
        Services.AddQuartz(quartz => quartz
            .AddJob<RunWorkflowJob>(job => job.WithIdentity(RunWorkflowJob.JobKey).StoreDurably())
            .AddJob<ResumeWorkflowJob>(job => job.WithIdentity(ResumeWorkflowJob.JobKey).StoreDurably()));
    }
}