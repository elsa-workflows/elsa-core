using System.Text.Json.Nodes;
using Elsa.IntegrationTests.Scenarios.JsonObjectJint.Activities;
using Elsa.JavaScript.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.IntegrationTests.Scenarios.JsonObjectJint.Workflows;

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
                new WriteLine(JavaScriptExpression.Create("getItem()['Foo']['Bar']"))
            }
        };
    }
}