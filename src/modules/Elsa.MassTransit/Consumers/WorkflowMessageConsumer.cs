using Elsa.MassTransit.Activities;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// A consumer of various message types to trigger activities derived from these messages.
/// </summary>
public class WorkflowMessageConsumer<T>(IStimulusSender stimulusSender) : IConsumer<T>
    where T : class
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<T> context)
    {
        var cancellationToken = context.CancellationToken;
        var messageType = typeof(T);
        var message = context.Message;
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName(messageType);
        var stimulus = new MessageReceivedBookmarkPayload(messageType);
        var correlationId = context.CorrelationId?.ToString();
        var input = new Dictionary<string, object>
        {
            [MessageReceived.InputKey] = message
        };
        var stimulusMetadata = new StimulusMetadata
        {
            CorrelationId = correlationId,
            Input = input
        };
        await stimulusSender.SendAsync(activityTypeName, stimulus, stimulusMetadata, cancellationToken);
    }
}