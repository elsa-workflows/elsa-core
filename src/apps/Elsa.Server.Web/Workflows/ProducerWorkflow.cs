using Elsa.Kafka.Activities;
using Elsa.Workflows;

namespace Elsa.Server.Web.Workflows;

public class ProducerWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Name = "Producer Workflow";
        builder.Root = new SendMessage
        {
            Topic = new("topic-2"),
            ProducerDefinitionId = new("producer-1"),
            Content = new(() => new
            {
                OrderId = "1",
                CustomerId = "1"
            })
        };
    }
}