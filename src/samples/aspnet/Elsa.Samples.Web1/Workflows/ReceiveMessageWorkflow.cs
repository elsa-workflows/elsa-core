using Elsa.Activities;
using Elsa.AzureServiceBus.Activities;
using Elsa.Models;
using Elsa.Services;

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
                }.CaptureOutput(receivedMessageVariable),
                new WriteLine(context => $"Message received: {receivedMessageVariable.Get(context)}")
            }
        });
    }
}