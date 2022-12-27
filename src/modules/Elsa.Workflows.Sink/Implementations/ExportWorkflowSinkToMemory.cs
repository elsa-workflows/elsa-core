using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;

namespace Elsa.Workflows.Sink.Implementations;

public class ExportWorkflowSinkToMemory : ICommandHandler<ExportWorkflowSinkMessage>, IConsumer<ExportWorkflowSinkMessage>
{
    private readonly IMemoryWorkflowSink _sinkProvider;
    private readonly IPrepareWorkflowSinkModel _prepareWorkflowSinkModel;

    public ExportWorkflowSinkToMemory(IMemoryWorkflowSink sinkProvider, IPrepareWorkflowSinkModel prepareWorkflowSinkModel)
    {
        _sinkProvider = sinkProvider;
        _prepareWorkflowSinkModel = prepareWorkflowSinkModel;
    }

    public async Task<Unit> HandleAsync(ExportWorkflowSinkMessage message, CancellationToken cancellationToken)
    {
        var workflowSinkModel = await _prepareWorkflowSinkModel.ExecuteAsync(message.WorkflowDefinitionId, message.WorkflowDefinitionVersion, message.WorkflowStateId, cancellationToken);
        await _sinkProvider.SaveAsync(workflowSinkModel, cancellationToken);
        return Unit.Instance;
    }

    public async ValueTask ConsumeAsync(ExportWorkflowSinkMessage message, CancellationToken cancellationToken)
    {
        var workflowSinkModel = await _prepareWorkflowSinkModel.ExecuteAsync(message.WorkflowDefinitionId, message.WorkflowDefinitionVersion, message.WorkflowStateId, cancellationToken);
        await _sinkProvider.SaveAsync(workflowSinkModel, cancellationToken);
    }
}