using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Parameters;
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
        object? payload = default,
        CancellationToken cancellationToken = default)
    {
        return await PublishInternalAsync(eventName, false, correlationId, workflowInstanceId, activityInstanceId, payload, cancellationToken);
    }

    private async Task<ICollection<WorkflowExecutionResult>> PublishInternalAsync(
        string eventName,
        bool dispatchAsynchronously,
        string? correlationId = default,
        string? workflowInstanceId = default,
        string? activityInstanceId = default,
        object? payload = default,
        CancellationToken cancellationToken = default)
    {
        var eventBookmark = new EventBookmarkPayload(eventName);
        var workflowInput = new Dictionary<string, object>
        {
            [Event.EventPayloadWorkflowInputKey] = payload ?? new Dictionary<string, object>()
        };
        var message = NewWorkflowInboxMessage.For<Event>(eventBookmark, workflowInstanceId, correlationId, activityInstanceId, workflowInput);
        var options = new WorkflowInboxMessageDeliveryParams
        {
            DispatchAsynchronously = dispatchAsynchronously
        };

        var result = await _workflowInbox.SubmitAsync(message, options, cancellationToken);
        return result.WorkflowExecutionResults;
    }
}