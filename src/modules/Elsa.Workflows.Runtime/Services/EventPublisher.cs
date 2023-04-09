using Elsa.Extensions;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class EventPublisher : IEventPublisher
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowDispatcher _workflowDispatcher;

    /// <summary>
    /// Constructor.
    /// </summary>
    public EventPublisher(IWorkflowRuntime workflowRuntime, IWorkflowDispatcher workflowDispatcher)
    {
        _workflowRuntime = workflowRuntime;
        _workflowDispatcher = workflowDispatcher;
    }
    
    /// <inheritdoc />
    public async Task PublishAsync(string eventName, string? correlationId = default, string? workflowInstanceId = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var eventBookmark = new EventBookmarkPayload(eventName);
        var options = new TriggerWorkflowsRuntimeOptions(correlationId, workflowInstanceId, input);
        await _workflowRuntime.TriggerWorkflowsAsync<Event>(eventBookmark, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DispatchAsync(string eventName, string? correlationId = default, string? workflowInstanceId = default, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var eventBookmark = new EventBookmarkPayload(eventName);
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<Event>();
        var request = new DispatchTriggerWorkflowsRequest(activityTypeName, eventBookmark, correlationId, workflowInstanceId, input);
        await _workflowDispatcher.DispatchAsync(request, cancellationToken);
    }
}