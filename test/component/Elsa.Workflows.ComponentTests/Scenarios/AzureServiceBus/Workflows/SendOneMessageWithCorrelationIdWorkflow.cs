using Elsa.AzureServiceBus.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Helpers.Activities;
using Elsa.Workflows.Contracts;
using NJsonSchema.Validation.FormatValidators;

namespace Elsa.Workflows.ComponentTests.Scenarios.AzureServiceBus.Workflows;

public class SendOneMessageWithCorrelationIdWorkflow : WorkflowBase
{
    public static readonly string Topic = nameof(SendOneMessageWithCorrelationIdWorkflow);
    public static readonly string CorrelationId = Guid.NewGuid().ToString();
    public static readonly object Signal1 = new();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new SendMessage
                {
                    QueueOrTopic = new(Topic),
                    MessageBody = new("Hello World"),
                },
                new Correlate
                {
                    CorrelationId = new(CorrelationId)
                },
                new MessageReceived(Topic, "subscription2"),
                new TriggerSignal(Signal1)
            }
        };
    }
}