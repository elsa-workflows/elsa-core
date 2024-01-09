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
        var message = context.Message;
        
        // TODO: Implement cancellation.
    }
}