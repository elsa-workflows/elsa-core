using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JsonObjectToObjectRemainsJsonObject.Activities;

public class ObjectProcessor : CodeActivity<object>
{
    public Input<object> Value { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var value = Value.Get(context);
        context.SetResult(value);
    }
}