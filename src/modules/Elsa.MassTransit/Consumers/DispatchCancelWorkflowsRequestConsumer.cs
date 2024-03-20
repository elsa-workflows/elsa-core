using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// Consumes a <see cref="DispatchCancelWorkflowsRequest"/> to cancel workflows.
/// </summary>
public class DispatchCancelWorkflowsRequestConsumer(IWorkflowCancellationService workflowCancellationService) : IConsumer<DispatchCancelWorkflowsRequest>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchCancelWorkflowsRequest> context)
    {
        var request = context.Message;
        var tasks = new List<Task<int>>();
        
        if (request.WorkflowInstanceIds is not null)
            tasks.Add(workflowCancellationService.CancelWorkflowsAsync(request.WorkflowInstanceIds!));
        if (request.DefinitionVersionId is not null)
            tasks.Add(workflowCancellationService.CancelWorkflowByDefinitionVersionAsync(request.DefinitionVersionId!));
        if (request.DefinitionId is not null)
            tasks.Add(workflowCancellationService.CancelWorkflowByDefinitionAsync(request.DefinitionId!, request.VersionOptions!.Value));

        await Task.WhenAll(tasks);
    }
}