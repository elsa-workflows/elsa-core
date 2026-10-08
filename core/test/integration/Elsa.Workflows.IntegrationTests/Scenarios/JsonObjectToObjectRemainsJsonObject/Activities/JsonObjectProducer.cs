using System.Text.Json.Nodes;
using Elsa.Extensions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JsonObjectToObjectRemainsJsonObject.Activities;

public class JsonObjectProducer : CodeActivity<JsonObject>
{
    protected override void Execute(ActivityExecutionContext context)
    {
        var value = new JsonObject
        {
            ["Foo"] = new JsonObject
            {
                ["Bar"] = "Baz"
            }
        };

        context.SetResult(value);
    }
}