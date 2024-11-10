using System.Dynamic;
using Elsa.Kafka.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Server.Web.Workflows;

public class ConsumerWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var message = builder.WithVariable<ExpandoObject>();
        builder.Name = "Consumer Workflow";
        builder.Root = new Sequence
        {
            Activities =
            {
                new MessageReceived
                {
                    ConsumerDefinitionId = new("consumer-1"),
                    MessageType = new(typeof(ExpandoObject)),
                    CorrelatingFields = new(() => new Dictionary<string, object?>
                    {
                        ["orderId"] = "1",
                        ["customerId"] = "1"
                    }),
                    Result = new(message),
                    CanStartWorkflow = true
                },
                new WriteLine(c => message.Get(c)!.ToString())
            }
        };
    }
}