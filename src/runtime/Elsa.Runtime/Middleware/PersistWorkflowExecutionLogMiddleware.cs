using Elsa.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Pipelines.WorkflowExecution.Components;

namespace Elsa.Runtime.Middleware;

/// <summary>
/// Takes care of persisting a workflow instance after workflow execution.
/// </summary>
public class PersistWorkflowExecutionLogMiddleware : WorkflowExecutionMiddleware
{
    private readonly ICommandSender _commandSender;
    private readonly IIdentityGenerator _identityGenerator;

    public PersistWorkflowExecutionLogMiddleware(WorkflowMiddlewareDelegate next, ICommandSender commandSender, IIdentityGenerator identityGenerator) : base(next)
    {
        _commandSender = commandSender;
        _identityGenerator = identityGenerator;
    }

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        // Persist workflow execution log entries.
        var entries = context.ExecutionLog.Select(x => new WorkflowExecutionLogRecord
        {
            Id = _identityGenerator.GenerateId(),
            ActivityId = x.NodeId,
            ActivityType = x.NodeType,
            Message = x.Message,
            EventName = x.EventName,
            WorkflowInstanceId = context.Id,
            Source = x.Source,
            Payload = x.Payload,
            Timestamp = x.Timestamp
        }).ToList();

        await _commandSender.ExecuteAsync(new SaveWorkflowExecutionLog(entries), context.CancellationToken);
    }
}