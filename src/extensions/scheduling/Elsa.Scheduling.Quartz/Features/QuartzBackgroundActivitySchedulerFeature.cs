using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Quartz.Features;

/// <summary>
/// Installs a Quartz.NET implementation for <see cref="IBackgroundActivityScheduler"/>.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
[UsedImplicitly]
public class QuartzBackgroundActivitySchedulerFeature : FeatureBase
{
    /// <inheritdoc />
    public QuartzBackgroundActivitySchedulerFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(workflowRuntimeFeature =>
        {
            workflowRuntimeFeature.BackgroundActivityScheduler = sp => sp.GetRequiredService<QuartzBackgroundActivityScheduler>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<QuartzBackgroundActivityScheduler>();
    }
}
