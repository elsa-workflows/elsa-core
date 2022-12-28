using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Sink.Implementations;

public class ExportWorkflowSink : INotificationHandler<ExportWorkflowSinkMessage>, global::MassTransit.IConsumer<ExportWorkflowSinkMessage>
{
    private readonly IEnumerable<IWorkflowSinkManager> _sinkManagers;
    private readonly IPrepareWorkflowSinkModel _prepareWorkflowSinkModel;
    private readonly ILogger _logger;

    public ExportWorkflowSink(
        IEnumerable<IWorkflowSinkManager> sinkManagers, 
        IPrepareWorkflowSinkModel prepareWorkflowSinkModel,
        ILogger<ExportWorkflowSink> logger)
    {
        _sinkManagers = sinkManagers;
        _prepareWorkflowSinkModel = prepareWorkflowSinkModel;
        _logger = logger;
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
                _logger.LogError("An error occured during sink export: {message}", ex.Message);
            }
        }
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
                _logger.LogError("An error occured during sink export: {message}", ex.Message);
            }
        }
    }
}