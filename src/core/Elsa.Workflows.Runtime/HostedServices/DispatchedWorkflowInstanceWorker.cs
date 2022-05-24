using System.Threading.Channels;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.HostedServices;

public class DispatchedWorkflowInstanceWorker : BackgroundService
{
    private readonly ChannelReader<DispatchWorkflowInstanceRequest> _channelReader;
    private readonly IWorkflowInvoker _workflowInvoker;
    private readonly ILogger _logger;

    public DispatchedWorkflowInstanceWorker(
        ChannelReader<DispatchWorkflowInstanceRequest> channelReader,
        IWorkflowInvoker workflowInvoker,
        ILogger<DispatchedWorkflowInstanceWorker> logger)
    {
        _channelReader = channelReader;
        _workflowInvoker = workflowInvoker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var (instanceId, bookmark, input, correlationId) in _channelReader.ReadAllAsync(cancellationToken))
        {
            var request = new InvokeWorkflowInstanceRequest(instanceId, bookmark, input, correlationId);

            try
            {
                await _workflowInvoker.InvokeAsync(request, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}