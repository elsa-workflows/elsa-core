using Elsa.AzureServiceBus.Activities;
using Elsa.Testing.Shared.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.AzureServiceBus.ComponentTests.Workflows;

public class SendOneMessageWorkflow : WorkflowBase
{
    public static readonly string Topic = nameof(SendOneMessageWorkflow);
    public static readonly object Signal1 = new();
    
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new Fork
                {
                    Branches =
                    {
                        new MessageReceived(Topic, "subscription1"),
                        new SendMessage
                        {
                            QueueOrTopic = new(Topic),
                            MessageBody = new("Hello World"),
                        }
                    }
                },
                new TriggerSignal(Signal1)
            }
        };
    }
}