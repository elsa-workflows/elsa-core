using System.Dynamic;
using System.Text.Json;
using Elsa.JavaScript.Models;
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
                    Topics = new(["topic-1"]),
                    Predicate = new(JavaScriptExpression.Create("message => message.OrderId == '1'")),
                    Result = new(message),
                    CanStartWorkflow = false
                },
                new WriteLine(c => JsonSerializer.Serialize(message.Get(c)))
            }
        };
    }
}