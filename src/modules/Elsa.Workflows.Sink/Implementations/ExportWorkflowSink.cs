using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Implementations;

public class ExportWorkflowSink : INotificationHandler<ExportWorkflowSinkMessage>, IConsumer<ExportWorkflowSinkMessage>
{
    private readonly IEnumerable<IWorkflowSinkManager> _sinkManagers;
    private readonly IPrepareWorkflowSinkModel _prepareWorkflowSinkModel;

    public ExportWorkflowSink(IEnumerable<IWorkflowSinkManager> sinkManagers, IPrepareWorkflowSinkModel prepareWorkflowSinkModel)
    {
        _sinkManagers = sinkManagers;
        _prepareWorkflowSinkModel = prepareWorkflowSinkModel;
    }

    public async Task HandleAsync(ExportWorkflowSinkMessage message, CancellationToken cancellationToken)
    {
        var workflowSinkModel = await _prepareWorkflowSinkModel.ExecuteAsync(message.WorkflowState, cancellationToken);

        foreach (var manager in _sinkManagers)
        {
            await manager.SaveAsync(workflowSinkModel, cancellationToken);
        }
    }

    public async ValueTask ConsumeAsync(ExportWorkflowSinkMessage message, CancellationToken cancellationToken)
    {
        var workflowSinkModel = await _prepareWorkflowSinkModel.ExecuteAsync(message.WorkflowState, cancellationToken);
        
        foreach (var manager in _sinkManagers)
        {
            await manager.SaveAsync(workflowSinkModel, cancellationToken);
        }
    }
}