using Elsa.Models;
using Elsa.AzureServiceBus.Activities;
using Elsa.Services;

namespace Elsa.Samples.Web1.Workflows;

public class SendMessageWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowDefinitionBuilder workflow)
    {
        workflow.WithRoot(new SendMessage
        {
            QueueOrTopic = new Input<string>("inbox"),
            MessageBody = new Input<object>(new { Subject = "Greetings", Message = "Hello World!" }),
            ContentType = new Input<string>("application/json")
        });
    }
}