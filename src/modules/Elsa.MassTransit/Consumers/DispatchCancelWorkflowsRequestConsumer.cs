using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// Consumes a <see cref="DispatchCancelWorkflowsRequest"/> to cancel workflows.
/// </summary>
public class DispatchCancelWorkflowsRequestConsumer(IWorkflowRuntime workflowRuntime) : IConsumer<DispatchCancelWorkflowsRequest>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchCancelWorkflowsRequest> context)
    {
        var request = context.Message;

        await workflowRuntime.CancelWorkflowAsync(request.WorkflowInstanceId);
    }
}