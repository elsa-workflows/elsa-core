using Elsa.Common.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
using Elsa.Scheduling.HostedServices;
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

    public override void ConfigureHostedServices()
    {
        Module.ConfigureHostedService<ScheduleWorkflowsHostedService>();
    }

    public override void Apply()
    {
        Services
            .AddSingleton<IWorkflowTriggerScheduler, WorkflowTriggerScheduler>()
            .AddSingleton<IWorkflowBookmarkScheduler, WorkflowBookmarkScheduler>()
            .AddNotificationHandlersFrom<ScheduleWorkflowsHostedService>();
    }
}