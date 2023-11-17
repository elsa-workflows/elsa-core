using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Hangfire.Handlers;
using Elsa.Hangfire.Services;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Features;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Hangfire.Features;

/// <summary>
/// Installs a Hangfire implementation for <see cref="IWorkflowScheduler"/>.
/// </summary>
[DependsOn(typeof(SchedulingFeature))]
public class HangfireSchedulerFeature : FeatureBase
{
    /// <inheritdoc />
    public HangfireSchedulerFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<SchedulingFeature>(schedulingFeature =>
        {
            schedulingFeature.WorkflowScheduler = sp => sp.GetRequiredService<HangfireWorkflowScheduler>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<HangfireWorkflowScheduler>();
        Services.AddSingleton<IActivityDescriptorModifier, CronActivityDescriptorModifier>();
    }
}