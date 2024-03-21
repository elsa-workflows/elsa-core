using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Requests;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// Consumes messages related to workflow definition changes.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionConsumer(IActivityRegistryPopulator activityRegistryPopulator) :
    IConsumer<RefreshWorkflowDefinitionsRequest>
{
    /// <summary>
    /// Consumes requests to refresh the workflow definitions.
    /// </summary>
    public Task Consume(ConsumeContext<RefreshWorkflowDefinitionsRequest> context)
    {
        return RefreshAsync();
    }
    
    private async Task RefreshAsync() => await activityRegistryPopulator.PopulateRegistryAsync(typeof(WorkflowDefinitionActivityProvider));
}