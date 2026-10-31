using CShells.Features;
using Elsa.ServiceBus.MassTransit.Consumers;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.Workflows.Management.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.ShellFeatures;

/// <summary>
/// Shell feature for distributed messaging support for workflow management using MassTransit.
/// </summary>
[ShellFeature(
    DisplayName = "MassTransit Workflow Management",
    Description = "Enables distributed messaging for workflow definition updates using MassTransit",
    DependsOn = [typeof(MassTransitFeature), typeof(WorkflowManagementFeature)])]
[UsedImplicitly]
public class MassTransitWorkflowManagementFeature(ShellFeatureContext context) : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        context.AddMassTransitConsumer<WorkflowDefinitionEventsConsumer>(
            endpointName: "elsa-workflow-definition-updates",
            isTemporary: true,
            ignoreConsumersDisabled: true);

        services.AddNotificationHandlersFrom(GetType());
    }
}
