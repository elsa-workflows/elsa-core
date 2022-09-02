using System.Threading.Channels;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.HostedServices;

public class DispatchedWorkflowInstanceWorker : BackgroundService
{
    private readonly ChannelReader<DispatchWorkflowInstanceRequest> _channelReader;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly ILogger _logger;

    public DispatchedWorkflowInstanceWorker(
        ChannelReader<DispatchWorkflowInstanceRequest> channelReader,
        IWorkflowRuntime workflowRuntime,
        ILogger<DispatchedWorkflowInstanceWorker> logger)
    {
        _channelReader = channelReader;
        _workflowRuntime = workflowRuntime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var (instanceId, bookmarkId, input, correlationId) in _channelReader.ReadAllAsync(cancellationToken))
        {
            var options = new ResumeWorkflowOptions(input);
            
            try
            {
                await _workflowRuntime.ResumeWorkflowAsync(instanceId, bookmarkId, options, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An unhandled exception occured while processing the queue");
            }
        }
    }
}