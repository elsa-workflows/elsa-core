using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Middleware;

/// <summary>
/// Takes care of persisting workflow execution log entries.
/// </summary>
public class PersistWorkflowExecutionLogMiddleware : WorkflowExecutionMiddleware
{
    private readonly IWorkflowExecutionLogStore _workflowExecutionLogStore;
    private readonly IIdentityGenerator _identityGenerator;

    /// <inheritdoc />
    public PersistWorkflowExecutionLogMiddleware(WorkflowMiddlewareDelegate next, IWorkflowExecutionLogStore workflowExecutionLogStore, IIdentityGenerator identityGenerator) : base(next)
    {
        _workflowExecutionLogStore = workflowExecutionLogStore;
        _identityGenerator = identityGenerator;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        // Persist workflow execution log entries.
        var entries = context.ExecutionLog.Select(x => new WorkflowExecutionLogRecord
        {
            Id = _identityGenerator.GenerateId(),
            ActivityInstanceId = x.ActivityInstanceId,
            ParentActivityInstanceId = x.ParentActivityInstanceId,
            ActivityId = x.ActivityId,
            ActivityType = x.ActivityType,
            Message = x.Message,
            EventName = x.EventName,
            WorkflowDefinitionId = context.Workflow.Identity.DefinitionId,
            WorkflowInstanceId = context.Id,
            WorkflowVersion = context.Workflow.Version,
            Source = x.Source,
            Payload = x.Payload,
            Timestamp = x.Timestamp
        }).ToList();

        await _workflowExecutionLogStore.SaveManyAsync(entries, context.CancellationToken);
    }
}