using Elsa.MassTransit.Activities;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// A consumer of various dispatch message types to asynchronously execute workflows.
/// </summary>
public class WorkflowMessageConsumer<T> : IConsumer<T> where T : class
{
    private readonly IWorkflowDispatcher _workflowRuntime;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowMessageConsumer(IWorkflowDispatcher workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<T> context)
    {
        var cancellationToken = context.CancellationToken;
        var messageType = typeof(T);
        var message = context.Message;
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName(messageType);
        var bookmark = new MessageReceivedBookmarkPayload(messageType);
        var correlationId = context.CorrelationId?.ToString();
        var input = new Dictionary<string, object> { [MessageReceived.InputKey] = message };
        var request = new DispatchTriggerWorkflowsRequest(activityTypeName, bookmark, correlationId, default, input);
        await _workflowRuntime.DispatchAsync(request, cancellationToken);
    }
}