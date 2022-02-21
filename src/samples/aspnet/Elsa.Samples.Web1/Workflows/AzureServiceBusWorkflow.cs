using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.AzureServiceBus.Activities;
using Elsa.Runtime.Contracts;

namespace Elsa.Samples.Web1.Workflows;

public class AzureServiceBusWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new Send
        {
            QueueOrTopic = new Input<string>("inbox"),
            MessageBody = new Input<object>(new { Subject = "Greetings", Message = "Hello World!" }),
            ContentType = new Input<string>("application/json")
        });
    }
}