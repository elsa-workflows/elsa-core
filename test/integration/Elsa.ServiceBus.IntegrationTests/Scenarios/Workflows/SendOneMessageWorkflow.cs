using Elsa.AzureServiceBus.Activities;
using Elsa.ServiceBus.IntegrationTests.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.ServiceBus.IntegrationTests.Scenarios.Workflows;

public class SendOneMessageWorkflow(ITestResetEventManager waitHandleTestManager) : WorkflowBase
{
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
                        new MessageReceived("topicName", "subscriptionName"),
                        new SendMessage
                        {
                            QueueOrTopic = new("sendTopic1"),
                            MessageBody = new("Hello World"),
                        }
                    }
                },
                new WriteLine(_ =>
                {
                    waitHandleTestManager.Set("receive1");
                    return "first receive ok";
                }),
            }
        };
    }
}