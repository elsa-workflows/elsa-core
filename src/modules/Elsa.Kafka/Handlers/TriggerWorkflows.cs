using System.Text.Json;
using Elsa.Kafka.Activities;
using Elsa.Kafka.Notifications;
using Elsa.Kafka.Stimuli;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime;

namespace Elsa.Kafka.Handlers;

public class TriggerWorkflows(IStimulusSender stimulusSender) : INotificationHandler<TransportMessageReceived>
{
    public async Task HandleAsync(TransportMessageReceived notification, CancellationToken cancellationToken)
    {
        var parsedMessage = JsonSerializer.Deserialize<JsonElement>(notification.TransportMessage.Value);
        var consumer = notification.Consumer;
        var consumerDefinition = consumer.ConsumerDefinition;
        var consumerDefinitionId = consumerDefinition.Id;
        var correlatingFieldNames = consumerDefinition.CorrelatingFields;
        var correlatingFields = new Dictionary<string, object?>();

        foreach (var correlatingFieldName in correlatingFieldNames)
            correlatingFields[correlatingFieldName] = GetCorrelatingFieldValue(parsedMessage, correlatingFieldName);

        var stimulus = new MessageReceivedStimulus(consumerDefinitionId, correlatingFields);
        var transportMessage = notification.TransportMessage;
        var metadata = new StimulusMetadata
        {
            Input = new Dictionary<string, object>
            {
                [MessageReceived.InputKey] = transportMessage
            }
        };
        await stimulusSender.SendAsync<MessageReceived>(stimulus, metadata, cancellationToken);
    }

    private object? GetCorrelatingFieldValue(JsonElement parsedMessage, string correlatingFieldName)
    {
        if (parsedMessage.TryGetProperty(correlatingFieldName, out var correlatingFieldValue))
        {
            // Switch on the value type:
            switch (correlatingFieldValue.ValueKind)
            {
                case JsonValueKind.String:
                    return correlatingFieldValue.GetString();
                case JsonValueKind.Number:
                    return correlatingFieldValue.GetInt32();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
            }
        }

        return null;
    }
}