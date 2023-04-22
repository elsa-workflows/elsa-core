using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Services;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Webhooks.Handlers;

/// <summary>
/// Handles the <see cref="RunTaskRequest"/> notification and asynchronously invokes all registered webhook endpoints.
/// </summary>
public class RunTaskHandler : INotificationHandler<RunTaskRequest>
{
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Constructor.
    /// </summary>
    public RunTaskHandler(IWebhookDispatcher webhookDispatcher, ISystemClock systemClock)
    {
        _webhookDispatcher = webhookDispatcher;
        _systemClock = systemClock;
    }
    
    /// <inheritdoc />
    public async Task HandleAsync(RunTaskRequest notification, CancellationToken cancellationToken)
    {
        var activityExecutionContext = notification.ActivityExecutionContext;
        var workflowExecutionContext = activityExecutionContext.WorkflowExecutionContext;
        var workflowInstanceId = workflowExecutionContext.Id;
        var correlationId = workflowExecutionContext.CorrelationId;
        var workflow = workflowExecutionContext.Workflow;
        var workflowDefinitionId = workflow.Identity.DefinitionId;
        var workflowName = workflow.WorkflowMetadata.Name;
        
        var payload = new RunTaskWebhook(
            workflowInstanceId,
            workflowDefinitionId,
            workflowName,
            correlationId,
            notification.TaskId, 
            notification.TaskName, 
            notification.TaskPayload
        );
        
        var now = _systemClock.UtcNow;
        var webhookEvent = new WebhookEvent("RunTask", payload, now);
        await _webhookDispatcher.DispatchAsync(webhookEvent, cancellationToken);
    }
}