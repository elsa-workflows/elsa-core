using System.Text.Json;
using Elsa.Kafka.Activities;
using Elsa.Kafka.Notifications;
using Elsa.Kafka.Stimuli;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime;

namespace Elsa.Kafka.Handlers;

public class TriggerWorkflows(IStimulusSender stimulusSender, ICorrelationStrategy correlationStrategy) : INotificationHandler<TransportMessageReceived>
{
    public async Task HandleAsync(TransportMessageReceived notification, CancellationToken cancellationToken)
    {
        var consumer = notification.Consumer;
        var consumerDefinition = consumer.ConsumerDefinition;
        var consumerDefinitionId = consumerDefinition.Id;
        var correlatingFields = GetCorrelatingFields(notification);
        var stimulus = new MessageReceivedStimulus(consumerDefinitionId, correlatingFields);
        var transportMessage = notification.TransportMessage;
        var metadata = new StimulusMetadata
        {
            CorrelationId = GetCorrelationId(transportMessage),
            Input = new Dictionary<string, object>
            {
                [MessageReceived.InputKey] = transportMessage
            }
        };
        await stimulusSender.SendAsync<MessageReceived>(stimulus, metadata, cancellationToken);
    }

    private string? GetCorrelationId(KafkaTransportMessage transportMessage)
    {
        return correlationStrategy.GetCorrelationId(transportMessage);
    }

    private IDictionary<string, object?> GetCorrelatingFields(TransportMessageReceived notification)
    {
        var consumer = notification.Consumer;
        var consumerDefinition = consumer.ConsumerDefinition;
        var correlatingFieldNames = consumerDefinition.CorrelatingFields;
        var correlatingFields = new Dictionary<string, object?>();
        var parsedMessage = JsonSerializer.Deserialize<JsonElement>(notification.TransportMessage.Value);

        foreach (var correlatingFieldName in correlatingFieldNames)
            correlatingFields[correlatingFieldName] = GetCorrelatingFieldValue(parsedMessage, correlatingFieldName);
        
        return correlatingFields;
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