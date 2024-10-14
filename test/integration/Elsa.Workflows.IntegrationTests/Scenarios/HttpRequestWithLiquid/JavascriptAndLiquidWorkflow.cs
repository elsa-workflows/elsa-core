using Elsa.Expressions.Models;
using Elsa.JavaScript.Activities;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows.IntegrationTests.Scenarios.HttpRequestWithLiquid;

/// <summary>
/// A workflow that use javascript to get some data, use them with some liquid expressions
/// </summary>
public class JavascriptAndLiquidWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var products = new Variable<object> { Name = "Products", StorageDriverType = typeof(WorkflowInstanceStorageDriver) };
        var product = new Variable<object> { Name = "Product", StorageDriverType = typeof(WorkflowInstanceStorageDriver) };

        builder.Root = new Sequence
        {
            Variables = { products, product },
            Activities =
            {
                new RunJavaScript
                {
                    Script = new(@"setProducts([{""id"":1, ""price"":12.99}, {""id"":2, ""price"":10}, {""id"":3, ""price"":1}])")
                },
                new WriteLine(new Expression("Liquid", "First product id: {{ Variables.Products[0].id }}")),
                new WriteLine(new Expression("Liquid", "First product price rounded: {{ Variables.Products[0].price | round }}")),
                new WriteLine(new Expression("Liquid", "First product as json: {{ Variables.Products[0] | json }}")),
                new WriteLine(new Expression("Liquid", "Second product id: {{ Variables.Products[1].id }}")),
                new RunJavaScript
                {
                    Script = new(@"variables.Product = {""id"":2, ""price"":10}")
                },
                new WriteLine(new Expression("Liquid", "Single product id: {{ Variables.Product.id }}")),
                new WriteLine(new Expression("Liquid", "Single product as json: {{ Variables.Product | json }}")),
            }
        };
    }
}