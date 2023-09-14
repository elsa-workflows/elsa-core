using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class EventPublisher : IEventPublisher
{
    private readonly IWorkflowInbox _workflowInbox;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EventPublisher(IWorkflowInbox workflowInbox)
    {
        _workflowInbox = workflowInbox;
    }

    /// <inheritdoc />
    public async Task<ICollection<WorkflowExecutionResult>> PublishAsync(
        string eventName,
        string? correlationId = default,
        string? workflowInstanceId = default,
        string? activityInstanceId = default,
        IDictionary<string, object>? input = default,
        CancellationToken cancellationToken = default)
    {
        return await PublishInternalAsync(eventName, NotificationStrategy.Sequential, correlationId, workflowInstanceId, activityInstanceId, input, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        string eventName,
        string? correlationId = default,
        string? workflowInstanceId = default,
        string? activityInstanceId = default,
        IDictionary<string, object>? input = default,
        CancellationToken cancellationToken = default)
    {
        await PublishInternalAsync(eventName, NotificationStrategy.FireAndForget, correlationId, workflowInstanceId, activityInstanceId, input, cancellationToken);
    }

    private async Task<ICollection<WorkflowExecutionResult>> PublishInternalAsync(
        string eventName,
        IEventPublishingStrategy publishingStrategy,
        string? correlationId = default,
        string? workflowInstanceId = default,
        string? activityInstanceId = default,
        IDictionary<string, object>? input = default,
        CancellationToken cancellationToken = default)
    {
        var eventBookmark = new EventBookmarkPayload(eventName);
        var message = NewWorkflowInboxMessage.For<Event>(eventBookmark, workflowInstanceId, correlationId, activityInstanceId, input);
        var options = new WorkflowInboxMessageDeliveryOptions
        {
            EventPublishingStrategy = publishingStrategy,
        };

        var result = await _workflowInbox.SubmitAsync(message, options, cancellationToken);
        return result.WorkflowExecutionResults;
    }
}