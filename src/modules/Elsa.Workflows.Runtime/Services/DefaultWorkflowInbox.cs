using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class DefaultWorkflowInbox : IWorkflowInbox
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowInboxStore _store;
    private readonly INotificationSender _notificationSender;
    private readonly ISystemClock _systemClock;
    private readonly IIdentityGenerator _identityGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultWorkflowInbox"/> class.
    /// </summary>
    public DefaultWorkflowInbox(IWorkflowRuntime workflowRuntime, IWorkflowInboxStore store, INotificationSender notificationSender, ISystemClock systemClock, IIdentityGenerator identityGenerator)
    {
        _workflowRuntime = workflowRuntime;
        _store = store;
        _notificationSender = notificationSender;
        _systemClock = systemClock;
        _identityGenerator = identityGenerator;
    }

    /// <inheritdoc />
    public async ValueTask<SubmitWorkflowInboxMessageResult> SubmitAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var defaultOptions = new WorkflowInboxMessageDeliveryOptions();
        return await SubmitAsync(message, defaultOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<SubmitWorkflowInboxMessageResult> SubmitAsync(WorkflowInboxMessage message, WorkflowInboxMessageDeliveryOptions options, CancellationToken cancellationToken = default)
    {
        if(message.Id == null!) message.Id = _identityGenerator.GenerateId();
        if(message.CreatedAt == default) message.CreatedAt = _systemClock.UtcNow;
        
        await _store.SaveAsync(message, cancellationToken);
        
        var strategy = options.EventPublishingStrategy;
        var workflowExecutionResults = new List<WorkflowExecutionResult>();
        var notification = new WorkflowInboxMessageReceived(message, workflowExecutionResults);
        await _notificationSender.SendAsync(notification, strategy, cancellationToken);
        var result = new SubmitWorkflowInboxMessageResult(workflowExecutionResults);
        return result;
    }

    /// <inheritdoc />
    public async ValueTask<DeliverWorkflowInboxMessageResult> DeliverAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var activityTypeName = message.ActivityTypeName;
        var correlationId = message.CorrelationId;
        var workflowInstanceId = message.WorkflowInstanceId;
        var bookmarkPayload = message.BookmarkPayload;
        var input = message.Input;
        var options = new TriggerWorkflowsRuntimeOptions(correlationId, workflowInstanceId, input);
        var result = await _workflowRuntime.TriggerWorkflowsAsync(activityTypeName, bookmarkPayload, options, cancellationToken);
        
        message.IsHandled = result.TriggeredWorkflows.Any();

        if (message.IsHandled)
        {
            message.AffectedWorkflowInstancesId = result.TriggeredWorkflows.Select(x => x.WorkflowInstanceId).ToList();
            message.HandledAt = _systemClock.UtcNow;
            await _store.SaveAsync(message, cancellationToken);
        }
        
        return new DeliverWorkflowInboxMessageResult(result.TriggeredWorkflows);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.FindManyAsync(filter, cancellationToken);
    }
}