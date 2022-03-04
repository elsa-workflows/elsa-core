using Elsa.Attributes;
using Elsa.Models;
using Elsa.Modules.AzureServiceBus.Models;

namespace Elsa.Modules.AzureServiceBus.Activities;

[Activity("Azure.ServiceBus.MessageReceived", "Executes when a message is received from the configured queue or topic and subscription", "Azure Service Bus")]
public class MessageReceived : TriggerActivity
{
    public Input<string> QueueOrTopic { get; set; } = default!;
    public Input<string>? Subscription { get; set; } = default!;

    protected override object GetPayload(TriggerIndexingContext context)
    {
        var queueOrTopic = context.Get(QueueOrTopic)!;
        var subscription = context.Get(Subscription);
        return new MessageReceivedTriggerPayload(queueOrTopic, subscription);
    }
}