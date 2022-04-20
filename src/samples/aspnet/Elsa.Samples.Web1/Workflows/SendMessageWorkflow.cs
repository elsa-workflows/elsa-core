using Elsa.Contracts;
using Elsa.Models;
using Elsa.Modules.AzureServiceBus.Activities;

namespace Elsa.Samples.Web1.Workflows;

public class SendMessageWorkflow : IWorkflow
{
    public void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new SendMessage
        {
            QueueOrTopic = new Input<string>("inbox"),
            MessageBody = new Input<object>(new { Subject = "Greetings", Message = "Hello World!" }),
            ContentType = new Input<string>("application/json")
        });
    }
}