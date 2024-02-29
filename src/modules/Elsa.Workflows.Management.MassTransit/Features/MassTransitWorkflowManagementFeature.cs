using System.Diagnostics.CodeAnalysis;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using Elsa.Workflows.Management.Features;
using Elsa.Extensions;
using Elsa.Workflows.Management.MassTransit.Consumers;
using Elsa.Workflows.Management.MassTransit.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.MassTransit.Features;

/// <summary>
/// A feature for implementing distributed messaging using MassTransit for workflow management.
/// </summary>
[DependsOn(typeof(MassTransitFeature))]
[DependsOn(typeof(WorkflowManagementFeature))]
public class MassTransitWorkflowManagementFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    [RequiresUnreferencedCode("The assembly containing the specified marker type will be scanned for activity types.")]
    public override void Configure()
    {
        Module.Configure<WorkflowManagementFeature>(feature => feature.WorkflowDefinitionDispatcherFactory = sp => sp.GetRequiredService<MassTransitWorkflowDefinitionDispatcher>());
        Module.AddMassTransitConsumer<WorkflowDefinitionConsumer>("elsa-workflow-definition-updates", true);
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddScoped<MassTransitWorkflowDefinitionDispatcher>();
    }
}