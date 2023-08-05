using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class EventPublisher : IEventPublisher
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IWorkflowInbox _workflowInbox;
    private readonly IBookmarkHasher _bookmarkHasher;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EventPublisher(IWorkflowRuntime workflowRuntime, IWorkflowDispatcher workflowDispatcher, IWorkflowInbox workflowInbox, IBookmarkHasher bookmarkHasher)
    {
        _workflowRuntime = workflowRuntime;
        _workflowDispatcher = workflowDispatcher;
        _workflowInbox = workflowInbox;
        _bookmarkHasher = bookmarkHasher;
    }

    /// <inheritdoc />
    public async Task PublishAsync(string eventName, string? correlationId = default, string? workflowInstanceId = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        // var eventBookmark = new EventBookmarkPayload(eventName);
        // var options = new TriggerWorkflowsRuntimeOptions(correlationId, workflowInstanceId, input);
        // await _workflowRuntime.TriggerWorkflowsAsync<Event>(eventBookmark, options, cancellationToken);

        await PublishInternalAsync(eventName, NotificationStrategy.Sequential, correlationId, workflowInstanceId, input, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DispatchAsync(string eventName, string? correlationId = default, string? workflowInstanceId = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        // var eventBookmark = new EventBookmarkPayload(eventName);
        // var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<Event>();
        // var request = new DispatchTriggerWorkflowsRequest(activityTypeName, eventBookmark, correlationId, workflowInstanceId, input);
        // await _workflowDispatcher.DispatchAsync(request, cancellationToken);

        await PublishInternalAsync(eventName, NotificationStrategy.Background, correlationId, workflowInstanceId, input, cancellationToken);
    }

    private async Task PublishInternalAsync(string eventName, IEventPublishingStrategy publishingStrategy, string? correlationId = default, string? workflowInstanceId = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var eventBookmark = new EventBookmarkPayload(eventName);
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<Event>();

        var message = new WorkflowInboxMessage
        {
            Input = input,
            BookmarkPayload = eventBookmark,
            Hash = _bookmarkHasher.Hash(activityTypeName, eventBookmark),
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityTypeName = activityTypeName
        };

        var options = new WorkflowInboxMessageDeliveryOptions
        {
            EventPublishingStrategy = publishingStrategy
        };

        await _workflowInbox.SubmitAsync(message, options, cancellationToken);
    }
}