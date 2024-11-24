using Elsa.Common.Multitenancy;
using Elsa.MassTransit.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// A consumer of various dispatch message types to asynchronously execute workflows.
/// </summary>
public class WorkflowMessageConsumer<T>(IWorkflowDispatcher workflowRuntime) : IConsumer<T>
    where T : class
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<T> context)
    {
        var cancellationToken = context.CancellationToken;
        var messageType = typeof(T);
        var message = context.Message;
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName(messageType);
        var bookmark = new MessageReceivedBookmarkPayload(messageType);
        var correlationId = context.CorrelationId?.ToString();
        var input = new Dictionary<string, object>
        {
            [MessageReceived.InputKey] = message
        };
        var request = new DispatchTriggerWorkflowsRequest(activityTypeName, bookmark)
        {
            CorrelationId = correlationId,
            Input = input
        };
        await workflowRuntime.DispatchAsync(request, cancellationToken: cancellationToken);
    }
}