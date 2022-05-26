using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Middleware;

/// <summary>
/// Processes collected bookmarks.
/// </summary>
public class PublishWorkflowExecutionEventsMiddleware : WorkflowExecutionMiddleware
{
    private readonly IEventPublisher _eventPublisher;

    public PublishWorkflowExecutionEventsMiddleware(WorkflowMiddlewareDelegate next, IEventPublisher eventPublisher) : base(next)
    {
        _eventPublisher = eventPublisher;
    }

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        await _eventPublisher.PublishAsync(new WorkflowExecuting(context));
        await Next(context);
        await _eventPublisher.PublishAsync(new WorkflowExecuted(context));
    }
}