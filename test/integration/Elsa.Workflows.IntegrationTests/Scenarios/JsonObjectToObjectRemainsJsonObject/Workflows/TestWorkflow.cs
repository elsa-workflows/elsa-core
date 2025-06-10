using System.Text.Json.Nodes;
using Elsa.Expressions.Models;
using Elsa.Expressions.JavaScript.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.IntegrationTests.Scenarios.JsonObjectToObjectRemainsJsonObject.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JsonObjectToObjectRemainsJsonObject.Workflows;

public class TestWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var item = builder.WithVariable<JsonObject>("item", default!);

        builder.Root = new Sequence
        {
            Activities =
            {
                new JsonObjectProducer
                {
                    Result = new(item)
                },
                new ObjectProcessor
                {
                    Value = new(JavaScriptExpression.Create("getItem()"), new MemoryBlockReference()),
                    Result = new(item)
                },
                new WriteLine(JavaScriptExpression.Create("getItem()['Foo']['Bar']"))
            }
        };
    }
}