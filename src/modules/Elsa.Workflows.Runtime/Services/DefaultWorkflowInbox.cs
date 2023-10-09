using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
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
    private readonly IWorkflowInboxMessageStore _messageStore;
    private readonly INotificationSender _notificationSender;
    private readonly ISystemClock _systemClock;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IBookmarkHasher _bookmarkHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultWorkflowInbox"/> class.
    /// </summary>
    public DefaultWorkflowInbox(
        IWorkflowRuntime workflowRuntime,
        IWorkflowInboxMessageStore messageStore,
        INotificationSender notificationSender,
        ISystemClock systemClock,
        IIdentityGenerator identityGenerator,
        IBookmarkHasher bookmarkHasher)
    {
        _workflowRuntime = workflowRuntime;
        _messageStore = messageStore;
        _notificationSender = notificationSender;
        _systemClock = systemClock;
        _identityGenerator = identityGenerator;
        _bookmarkHasher = bookmarkHasher;
    }

    /// <inheritdoc />
    public async ValueTask<SubmitWorkflowInboxMessageResult> SubmitAsync(NewWorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var defaultOptions = new WorkflowInboxMessageDeliveryOptions();
        return await SubmitAsync(message, defaultOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<SubmitWorkflowInboxMessageResult> SubmitAsync(NewWorkflowInboxMessage newMessage, WorkflowInboxMessageDeliveryOptions options, CancellationToken cancellationToken = default)
    {
        var now = _systemClock.UtcNow;

        // Create a new message.
        var message = new WorkflowInboxMessage
        {
            Id = _identityGenerator.GenerateId(),
            CreatedAt = now,
            ExpiresAt = now + newMessage.TimeToLive,
            ActivityInstanceId = newMessage.ActivityInstanceId,
            CorrelationId = newMessage.CorrelationId,
            WorkflowInstanceId = newMessage.WorkflowInstanceId,
            ActivityTypeName = newMessage.ActivityTypeName,
            BookmarkPayload = newMessage.BookmarkPayload,
            Input = newMessage.Input,
            Hash = _bookmarkHasher.Hash(newMessage.ActivityTypeName, newMessage.BookmarkPayload, newMessage.ActivityInstanceId),
        };

        // Store the message.
        await _messageStore.SaveAsync(message, cancellationToken);

        // Send a notification.
        var strategy = options.EventPublishingStrategy;
        var workflowExecutionResults = new List<WorkflowExecutionResult>();
        var notification = new WorkflowInboxMessageReceived(message, workflowExecutionResults);
        await _notificationSender.SendAsync(notification, strategy, cancellationToken);

        // Return the result.
        return new SubmitWorkflowInboxMessageResult(message, workflowExecutionResults);
    }

    /// <inheritdoc />
    public async ValueTask<DeliverWorkflowInboxMessageResult> DeliverAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var resumedWorkflowResults = await ResumeWorkflowsAsync(message, cancellationToken);
        return new DeliverWorkflowInboxMessageResult(resumedWorkflowResults);
    }

    /// <inheritdoc />
    public async ValueTask<DeliverWorkflowInboxMessageResult> BroadcastAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var triggeredWorkflows = await TriggerWorkflowsAsync(message, cancellationToken);
        
        return new DeliverWorkflowInboxMessageResult(triggeredWorkflows);
    }

    private async Task<ICollection<WorkflowExecutionResult>> TriggerWorkflowsAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var activityTypeName = message.ActivityTypeName;
        var correlationId = message.CorrelationId;
        var workflowInstanceId = message.WorkflowInstanceId;
        var activityInstanceId = message.ActivityInstanceId;
        var bookmarkPayload = message.BookmarkPayload;
        var input = message.Input;
        var options = new TriggerWorkflowsOptions(correlationId, workflowInstanceId, activityInstanceId, input, cancellationToken);

        if (workflowInstanceId != null)
            return await _workflowRuntime.ResumeWorkflowsAsync(activityTypeName, bookmarkPayload, options);

        var result = await _workflowRuntime.TriggerWorkflowsAsync(activityTypeName, bookmarkPayload, options);
        return result.TriggeredWorkflows;
    }

    private async Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var activityTypeName = message.ActivityTypeName;
        var correlationId = message.CorrelationId;
        var workflowInstanceId = message.WorkflowInstanceId;
        var activityInstanceId = message.ActivityInstanceId;
        var bookmarkPayload = message.BookmarkPayload;
        var input = message.Input;
        var options = new TriggerWorkflowsOptions(correlationId, workflowInstanceId, activityInstanceId, input, cancellationToken);

        return await _workflowRuntime.ResumeWorkflowsAsync(activityTypeName, bookmarkPayload, options);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        return await _messageStore.FindManyAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(IEnumerable<WorkflowInboxMessageFilter> filters, CancellationToken cancellationToken = default)
    {
        return await _messageStore.FindManyAsync(filters, cancellationToken);
    }
}