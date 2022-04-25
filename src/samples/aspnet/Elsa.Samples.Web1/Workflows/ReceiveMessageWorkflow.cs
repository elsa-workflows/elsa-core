using Elsa.Activities;
using Elsa.Models;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.AzureServiceBus.Activities;
using Elsa.Services;

namespace Elsa.Samples.Web1.Workflows;

public class ReceiveMessageWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
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
                }.CaptureOutput(receivedMessageVariable),
                new WriteLine(context => $"Message received: {receivedMessageVariable.Get(context)}")
            }
        });
    }
}