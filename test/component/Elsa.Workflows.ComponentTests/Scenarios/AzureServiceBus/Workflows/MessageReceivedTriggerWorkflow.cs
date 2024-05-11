using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Helpers.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.ComponentTests.Scenarios.AzureServiceBus.Workflows;

public class MessageReceivedTriggerWorkflow : WorkflowBase
{
    public static readonly string Topic = nameof(MessageReceivedTriggerWorkflow);
    public static readonly object Signal1 = new();
    public static readonly object Signal2 = new();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(GetType().FullName!);
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