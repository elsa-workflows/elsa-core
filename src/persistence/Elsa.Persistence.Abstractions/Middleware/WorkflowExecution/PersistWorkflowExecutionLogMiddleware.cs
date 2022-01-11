using Elsa.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Pipelines.WorkflowExecution;

namespace Elsa.Persistence.Middleware.WorkflowExecution;

public static class PersistWorkflowExecutionLogMiddlewareExtensions
{
    public static IWorkflowExecutionBuilder PersistWorkflowExecutionLog(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistWorkflowExecutionLogMiddleware>();
}

/// <summary>
/// Takes care of persisting a workflow instance after workflow execution.
/// </summary>
public class PersistWorkflowExecutionLogMiddleware : IWorkflowExecutionMiddleware
{
    private readonly WorkflowMiddlewareDelegate _next;
    private readonly ICommandSender _commandSender;
    private readonly IIdentityGenerator _identityGenerator;

    public PersistWorkflowExecutionLogMiddleware(WorkflowMiddlewareDelegate next, ICommandSender commandSender, IIdentityGenerator identityGenerator)
    {
        _next = next;
        _commandSender = commandSender;
        _identityGenerator = identityGenerator;
    }

    public async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await _next(context);

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