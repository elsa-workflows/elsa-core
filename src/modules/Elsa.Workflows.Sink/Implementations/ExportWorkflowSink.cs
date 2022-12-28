using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;
using MassTransit;

namespace Elsa.Workflows.Sink.Implementations;

public class ExportWorkflowSink : INotificationHandler<ExportWorkflowSinkMessage>, global::MassTransit.IConsumer<ExportWorkflowSinkMessage>
{
    private readonly IEnumerable<IWorkflowSinkManager> _sinkManagers;
    private readonly IPrepareWorkflowSinkModel _prepareWorkflowSinkModel;

    private readonly IList<Error> _errors = new List<Error>();

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
            try
            {
                await manager.SaveAsync(workflowSinkModel, cancellationToken);
            }
            catch (Exception ex)
            {
                _errors.Add(new Error(ex.Message));
            }
        }
        
        CheckForErrors();
    }

    public async Task Consume(ConsumeContext<ExportWorkflowSinkMessage> context)
    {
        var workflowSinkModel = await _prepareWorkflowSinkModel.ExecuteAsync(context.Message.WorkflowState, context.CancellationToken);
        
        foreach (var manager in _sinkManagers)
        {
            try
            {
                await manager.SaveAsync(workflowSinkModel, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _errors.Add(new Error(ex.Message));
            }
        }

        CheckForErrors();
    }

    private void CheckForErrors()
    {
        if (_errors.Any())
        {
            throw new SinkExportFailed(string.Join(",", _errors.Select(e => e.Message)));
        }
    }
}