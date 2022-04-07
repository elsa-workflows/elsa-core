using Elsa.Activities;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.Activities.Activities.Console;
using Elsa.Modules.AzureServiceBus.Activities;
using Elsa.Runtime.Contracts;

namespace Elsa.Samples.Web1.Workflows;

public class ReceiveMessageWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        var receivedMessage = new Variable<string>();

        workflow.WithRoot(new Sequence
        {
            Variables = { receivedMessage },
            Activities =
            {
                new MessageReceived
                {
                    CanStartWorkflow = true,
                    QueueOrTopic = new Input<string>("inbox"),
                    Result = new Output<object?>(receivedMessage)
                },
                new WriteLine(context => $"Message received: {receivedMessage.Get(context)}")
            }
        });
    }
}