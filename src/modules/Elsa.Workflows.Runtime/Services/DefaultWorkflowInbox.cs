using Elsa.Common.Contracts;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class DefaultWorkflowInbox : IWorkflowInbox
{
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowInboxMessageStore _messageStore;
    private readonly INotificationSender _notificationSender;
    private readonly ISystemClock _systemClock;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IStimulusHasher _stimulusHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultWorkflowInbox"/> class.
    /// </summary>
    public DefaultWorkflowInbox(
        IWorkflowDispatcher workflowDispatcher,
        IWorkflowRuntime workflowRuntime,
        IWorkflowInboxMessageStore messageStore,
        INotificationSender notificationSender,
        ISystemClock systemClock,
        IIdentityGenerator identityGenerator,
        IStimulusHasher stimulusHasher)
    {
        _workflowDispatcher = workflowDispatcher;
        _workflowRuntime = workflowRuntime;
        _messageStore = messageStore;
        _notificationSender = notificationSender;
        _systemClock = systemClock;
        _identityGenerator = identityGenerator;
        _stimulusHasher = stimulusHasher;
    }

    /// <inheritdoc />
    public async ValueTask<SubmitWorkflowInboxMessageResult> SubmitAsync(NewWorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var defaultOptions = new WorkflowInboxMessageDeliveryParams();
        return await SubmitAsync(message, defaultOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<SubmitWorkflowInboxMessageResult> SubmitAsync(NewWorkflowInboxMessage newMessage, WorkflowInboxMessageDeliveryParams @params, CancellationToken cancellationToken = default)
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
            Hash = _stimulusHasher.Hash(newMessage.ActivityTypeName, newMessage.BookmarkPayload, newMessage.ActivityInstanceId),
        };

        // Store the message.
        await _messageStore.SaveAsync(message, cancellationToken);

        // Send a notification.
        var workflowExecutionResults = new List<WorkflowExecutionResult>();
        var notification = new WorkflowInboxMessageReceived(message, @params, workflowExecutionResults);
        await _notificationSender.SendAsync(notification, NotificationStrategy.Sequential, cancellationToken);

        // Return the result.
        return new SubmitWorkflowInboxMessageResult(message, workflowExecutionResults);
    }

    /// <inheritdoc />
    public async ValueTask<DeliverWorkflowInboxMessageResult> DeliverAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        await ResumeWorkflowsAsynchronouslyAsync(message, cancellationToken);
        return new DeliverWorkflowInboxMessageResult(new List<WorkflowExecutionResult>());
    }

    /// <inheritdoc />
    public async ValueTask<DeliverWorkflowInboxMessageResult> BroadcastAsync(WorkflowInboxMessage message, BroadcastWorkflowInboxMessageOptions? options, CancellationToken cancellationToken = default)
    {
        var activityTypeName = message.ActivityTypeName;
        var correlationId = message.CorrelationId;
        var workflowInstanceId = message.WorkflowInstanceId;
        var activityInstanceId = message.ActivityInstanceId;
        var bookmarkPayload = message.BookmarkPayload;
        var input = message.Input;

        if (workflowInstanceId != null)
        {
            if (options?.DispatchAsynchronously == true)
            {
                await ResumeWorkflowsAsynchronouslyAsync(message, cancellationToken);
                return new DeliverWorkflowInboxMessageResult(new List<WorkflowExecutionResult>());
            }

            var results = await ResumeWorkflowsSynchronouslyAsync(message, cancellationToken);
            return new DeliverWorkflowInboxMessageResult(results.ToList());
        }

        if (options?.DispatchAsynchronously == false)
        {
            var results = await _workflowRuntime.TriggerWorkflowsAsync(activityTypeName, bookmarkPayload, new TriggerWorkflowsOptions
            {
                CorrelationId = correlationId,
                WorkflowInstanceId = workflowInstanceId,
                ActivityInstanceId = activityInstanceId,
                Input = input,
                CancellationToken = cancellationToken
            });

            return new DeliverWorkflowInboxMessageResult(results.TriggeredWorkflows);
        }

        await _workflowDispatcher.DispatchAsync(new DispatchTriggerWorkflowsRequest(activityTypeName, bookmarkPayload)
        {
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId,
            Input = input
        }, cancellationToken: cancellationToken);

        return new DeliverWorkflowInboxMessageResult(new List<WorkflowExecutionResult>());
    }

    private async Task ResumeWorkflowsAsynchronouslyAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var activityTypeName = message.ActivityTypeName;
        var correlationId = message.CorrelationId;
        var workflowInstanceId = message.WorkflowInstanceId;
        var activityInstanceId = message.ActivityInstanceId;
        var bookmarkPayload = message.BookmarkPayload;
        var input = message.Input;

        await _workflowDispatcher.DispatchAsync(new DispatchResumeWorkflowsRequest(activityTypeName, bookmarkPayload)
        {
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId,
            Input = input
        }, cancellationToken: cancellationToken);
    }

    private async Task<IEnumerable<WorkflowExecutionResult>> ResumeWorkflowsSynchronouslyAsync(WorkflowInboxMessage message, CancellationToken cancellationToken = default)
    {
        var activityTypeName = message.ActivityTypeName;
        var correlationId = message.CorrelationId;
        var workflowInstanceId = message.WorkflowInstanceId;
        var activityInstanceId = message.ActivityInstanceId;
        var bookmarkPayload = message.BookmarkPayload;
        var input = message.Input;

        return await _workflowRuntime.ResumeWorkflowsAsync(activityTypeName, bookmarkPayload, new TriggerWorkflowsOptions
        {
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId,
            Input = input,
            CancellationToken = cancellationToken
        });
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