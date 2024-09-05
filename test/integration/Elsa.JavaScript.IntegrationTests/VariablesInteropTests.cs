using System.Dynamic;
using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.JavaScript.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

public class VariablesInteropTests(ITestOutputHelper testOutputHelper)
{
    private readonly IServiceProvider _serviceProvider = new TestApplicationBuilder(testOutputHelper).Build();

    [Fact(DisplayName = "Serialize large unicode string using JavaScript's toJson function")]
    public async Task WorkflowVariables_CanBeModified_Inline()
    {
        var javaScriptEvaluator = _serviceProvider.GetRequiredService<IJavaScriptEvaluator>();
        var workflowGraphBuilder = _serviceProvider.GetRequiredService<IWorkflowGraphBuilder>();
        var script = """
                     var payload = variables.MyPayload;
                     payload.text = 'Hello, World!';
                     payload.name = 'Jane Doe';
                     payload.orders.push({ id: '3', product: 'Product 3', price: 250 });
                     payload.orders.sort((a, b) => a.price - b.price);
                     """;
        var workflow = new Workflow();
        workflow.Variables.Add(new Variable("MyPayload", CreatePayload()));
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow);
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(_serviceProvider, workflowGraph, "1");
        var expressionExecutionContext = workflowExecutionContext.ExpressionExecutionContext!;
        var result = (IDictionary<string, object?>)(await javaScriptEvaluator.EvaluateAsync(script, typeof(ExpandoObject), expressionExecutionContext))!;
        var lastOrder = (IDictionary<string, object?>)((object[])((IDictionary<string, object?>)result["payload"])["orders"]!).Last();
        Assert.Equal(250m, (decimal)lastOrder["price"]!);
    }

    private ExpandoObject CreatePayload()
    {
        var value = new
        {
            Id = "123",
            Name = "John Doe",
            Age = 30,
            Orders = new[]
            {
                new
                {
                    Id = "1",
                    Product = "Product 1",
                    Price = 200m
                },
                new
                {
                    Id = "2",
                    Product = "Product 2",
                    Price = 100m
                }
            }
        };
        
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions{ PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
        return JsonSerializer.Deserialize<ExpandoObject>(json)!;
    }
}