using Elsa.Common.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Scheduling.Handlers;
using Elsa.Scheduling.Implementations;
using Elsa.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Features;

[DependsOn(typeof(SystemClockFeature))]
public class SchedulingFeature : FeatureBase
{
    public SchedulingFeature(IModule module) : base(module)
    {
    }

    public override void Apply()
    {
        Services
            .AddSingleton<IWorkflowTriggerScheduler, WorkflowTriggerScheduler>()
            .AddSingleton<IWorkflowBookmarkScheduler, WorkflowBookmarkScheduler>()
            .AddNotificationHandlersFrom<ScheduleWorkflows>();
    }
}