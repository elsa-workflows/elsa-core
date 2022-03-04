using Elsa.Activities.Console;
using Elsa.Activities.Workflows;
using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.AzureServiceBus.Activities;
using Elsa.Modules.AzureServiceBus.Models;
using Elsa.Runtime.Contracts;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SQLitePCL;

namespace Elsa.Samples.Web1.Workflows;

public class ReceiveMessageWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        // var receivedMessage = new Variable<string>();
        // workflow.Variables.Add(receivedMessage);

        workflow.AddTrigger(new MessageReceived
        {
            QueueOrTopic = new Input<string>("inbox"),
            //ReceivedMessageBody = new Output<object>(receivedMessage)
        });

        workflow.WithRoot(new Sequence
        {
            Activities =
            {
                new WriteLine(context => $"Message received: {context.Input}")
            }
        });
    }
}