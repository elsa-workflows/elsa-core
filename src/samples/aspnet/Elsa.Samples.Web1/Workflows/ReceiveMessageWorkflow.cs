using Elsa.Activities.Console;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.AzureServiceBus.Activities;
using Elsa.Modules.AzureServiceBus.Models;
using Elsa.Runtime.Contracts;

namespace Elsa.Samples.Web1.Workflows;

public class ReceiveMessageWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.AddTrigger(new MessageReceived
        {
            QueueOrTopic = new Input<string>("inbox")
        });

        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine("Message received!")
            }
        });
    }
}