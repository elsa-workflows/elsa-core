using Elsa.AzureServiceBus.Activities;
using Elsa.AzureServiceBus.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Web1.Workflows;

public class ReceiveMessageWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        var receivedMessageVariable = new Variable<string>();

        workflow.WithRoot(new Sequence
        {
            Variables = { receivedMessageVariable },
            Activities =
            {
                new MessageReceived
                {
                    CanStartWorkflow = true,
                    QueueOrTopic = new Input<string>("inbox"),
                    ReceivedMessage = new Output<ReceivedServiceBusMessageModel>(receivedMessageVariable)
                },
                new WriteLine(context => $"Message received: {receivedMessageVariable.Get(context)}")
            }
        });
    }
}