using Elsa.ServiceBus.AzureServiceBus.Activities;
using Elsa.ServiceBus.AzureServiceBus.Models;
using Elsa.Testing.Shared.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.ServiceBus.AzureServiceBus.ComponentTests.Workflows;

public class MessageReceivedTriggerWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();
    public static readonly string Topic = nameof(MessageReceivedTriggerWorkflow);
    public static readonly string Signal1 = "signal-1";
    public static readonly string Signal2 = "signal-2"; 

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);
        var message = builder.WithVariable<ReceivedServiceBusMessageModel>();
        builder.Root = new Sequence
        {
            Activities =
            {
                new MessageReceived(Topic, "subscription1")
                {
                    CanStartWorkflow = true,
                    TransportMessage = new(message)
                },
                new Correlate(context => message.Get(context)!.CorrelationId),
                new TriggerSignal(Signal1),
                new MessageReceived(Topic, "subscription2"),
                new TriggerSignal(Signal2)
            }
        };
    }
}