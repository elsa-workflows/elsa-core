using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Consumers;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Features;

/// <summary>
/// A feature for implementing distributed messaging using MassTransit for workflow management.
/// </summary>
[DependsOn(typeof(MassTransitFeature))]
[DependsOn(typeof(WorkflowManagementFeature))]
public class MassTransitWorkflowManagementFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddMassTransitConsumer<WorkflowDefinitionEventsConsumer>("elsa-workflow-definition-updates", true, true);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddNotificationHandlersFrom(GetType());
    }
}