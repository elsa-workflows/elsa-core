using System.Threading.Channels;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

/// <summary>
/// A simple implementation that queues the specified request for workflow execution on a non-durable background worker.
/// </summary>
public class TaskBasedWorkflowDispatcher : IWorkflowDispatcher
{
    private readonly ChannelWriter<DispatchWorkflowDefinitionRequest> _workflowDefinitionChannelWriter;
    private readonly ChannelWriter<DispatchWorkflowInstanceRequest> _workflowInstanceChannelWriter;

    public TaskBasedWorkflowDispatcher(
        ChannelWriter<DispatchWorkflowDefinitionRequest> workflowDefinitionChannelWriter,
        ChannelWriter<DispatchWorkflowInstanceRequest> workflowInstanceChannelWriter)
    {
        _workflowDefinitionChannelWriter = workflowDefinitionChannelWriter;
        _workflowInstanceChannelWriter = workflowInstanceChannelWriter;
    }
    
    public async Task<DispatchWorkflowDefinitionResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        await _workflowDefinitionChannelWriter.WriteAsync(request, cancellationToken);
        return new DispatchWorkflowDefinitionResponse();
    }

    public async Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        await _workflowInstanceChannelWriter.WriteAsync(request, cancellationToken);
        return new DispatchWorkflowInstanceResponse();
    }
}