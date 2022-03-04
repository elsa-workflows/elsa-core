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
        var receivedMessage = new Variable<string>();

        workflow.WithRoot(new Sequence
        {
            Variables = { receivedMessage },
            Activities =
            {
                new MessageReceived
                {
                    QueueOrTopic = new Input<string>("inbox"),
                    ReceivedMessageBody = new Output<object>(receivedMessage)
                },
                new WriteLine(context => $"Message received: {receivedMessage.Get(context)}")
            }
        });
    }
}