using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Common.Models;
using Elsa.Mediator.Services;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Sinks.Implementations;

public class ExportWorkflowSink : INotificationHandler<ExportWorkflowSinkMessage>, global::MassTransit.IConsumer<ExportWorkflowSinkMessage>
{
    private readonly IEnumerable<IWorkflowSinkClient> _sinkClients;
    private readonly IPrepareWorkflowInstance _prepareWorkflowInstance;
    private readonly ILogger _logger;

    public ExportWorkflowSink(
        IEnumerable<IWorkflowSinkClient> sinkClients, 
        IPrepareWorkflowInstance prepareWorkflowInstance,
        ILogger<ExportWorkflowSink> logger)
    {
        _sinkClients = sinkClients;
        _prepareWorkflowInstance = prepareWorkflowInstance;
        _logger = logger;
    }

    public async Task HandleAsync(ExportWorkflowSinkMessage message, CancellationToken cancellationToken)
    {
        var workflowSinkModel = await _prepareWorkflowInstance.ExecuteAsync(message.WorkflowState, cancellationToken);

        foreach (var client in _sinkClients)
        {
            try
            {
                await client.SaveAsync(workflowSinkModel, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occured during sink export: {message}", ex.Message);
            }
        }
    }

    public async Task Consume(ConsumeContext<ExportWorkflowSinkMessage> context)
    {
        var workflowSinkModel = await _prepareWorkflowInstance.ExecuteAsync(context.Message.WorkflowState, context.CancellationToken);
        
        foreach (var client in _sinkClients)
        {
            try
            {
                await client.SaveAsync(workflowSinkModel, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occured during sink export: {message}", ex.Message);
            }
        }
    }
}