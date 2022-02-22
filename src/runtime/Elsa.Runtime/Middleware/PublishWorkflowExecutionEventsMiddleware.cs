using Elsa.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Pipelines.WorkflowExecution.Components;
using Elsa.Runtime.Notifications;

namespace Elsa.Runtime.Middleware;

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