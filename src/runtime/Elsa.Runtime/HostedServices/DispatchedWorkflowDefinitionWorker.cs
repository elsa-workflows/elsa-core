using System.Threading.Channels;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Runtime.HostedServices;

public class DispatchedWorkflowDefinitionWorker : BackgroundService
{
    private readonly ChannelReader<DispatchWorkflowDefinitionRequest> _channelReader;
    private readonly IWorkflowInvoker _workflowInvoker;
    private readonly ILogger _logger;

    public DispatchedWorkflowDefinitionWorker(
        ChannelReader<DispatchWorkflowDefinitionRequest> channelReader,
        IWorkflowInvoker workflowInvoker,
        ILogger<DispatchedWorkflowDefinitionWorker> logger)
    {
        _channelReader = channelReader;
        _workflowInvoker = workflowInvoker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await foreach (var (definitionId, versionOptions, input, correlationId) in _channelReader.ReadAllAsync(cancellationToken))
        {
            var request = new InvokeWorkflowDefinitionRequest(definitionId, versionOptions, input, correlationId);

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