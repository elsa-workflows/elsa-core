using System.Text.Json.Nodes;
using Elsa.Expressions.Models;
using Elsa.IntegrationTests.Scenarios.JsonObjectToObjectRemainsJsonObject.Activities;
using Elsa.JavaScript.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.IntegrationTests.Scenarios.JsonObjectToObjectRemainsJsonObject.Workflows;

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