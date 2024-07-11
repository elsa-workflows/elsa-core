using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// Consumes a <see cref="DispatchCancelWorkflowRequest"/> to cancel workflows.
/// </summary>
[UsedImplicitly]
public class DispatchCancelWorkflowsRequestConsumer(IWorkflowRuntime workflowRuntime) : IConsumer<DispatchCancelWorkflowRequest>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchCancelWorkflowRequest> context)
    {
        var cancellationToken = context.CancellationToken;
        var request = context.Message;

        var client = await workflowRuntime.CreateClientAsync(request.WorkflowInstanceId, cancellationToken);
        await client.CancelAsync(cancellationToken);
    }
}