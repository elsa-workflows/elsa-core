using System.Text.RegularExpressions;
using Elsa.Expressions.JavaScript.TypeDefinitions.Contracts;
using Elsa.Expressions.JavaScript.TypeDefinitions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

public class VariableAccessorTypeDefinitionTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "Type definitions declare an underscore variable's accessors")]
    public async Task UnderscoreVariableAccessorsAreDeclared()
    {
        var typeDefinitions = await GenerateTypeDefinitionsAsync(new Variable<string>("order_id", "") { Id = "orderId" });

        Assert.Contains("declare function getOrderId(): string;", typeDefinitions);
        Assert.Contains("declare function setOrderId(value: string);", typeDefinitions);
    }

    [Fact(DisplayName = "Type definitions declare a colliding accessor once, for the underscore-free variable")]
    public async Task CollidingAccessorsAreDeclaredOnce()
    {
        var typeDefinitions = await GenerateTypeDefinitionsAsync(
            new Variable<string>("meting_context", "") { Id = "underscoreContext" },
            new Variable<int>("metingContext", 0) { Id = "plainContext" },
            new Variable<string>("_foo", "") { Id = "underscoreFoo" },
            new Variable<int>("foo", 0) { Id = "plainFoo" });

        Assert.Equal(["declare function getMetingContext(): number;"], Declarations(typeDefinitions, "getMetingContext"));
        Assert.Equal(["declare function setMetingContext(value: number);"], Declarations(typeDefinitions, "setMetingContext"));
        Assert.Equal(["declare function getFoo(): number;"], Declarations(typeDefinitions, "getFoo"));
        Assert.Equal(["declare function setFoo(value: number);"], Declarations(typeDefinitions, "setFoo"));
    }

    private static IEnumerable<string> Declarations(string typeDefinitions, string functionName) =>
        Regex.Matches(typeDefinitions, $@"declare function {functionName}\(.*;").Select(x => x.Value);

    private async Task<string> GenerateTypeDefinitionsAsync(params Variable[] variables)
    {
        await _fixture.BuildAsync();
        var workflow = new Workflow { Variables = variables.ToList() };
        var workflowGraphBuilder = _fixture.Services.GetRequiredService<IWorkflowGraphBuilder>();
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow);
        var typeDefinitionContext = new TypeDefinitionContext(workflowGraph, null, null, CancellationToken.None);
        var typeDefinitionService = _fixture.Services.GetRequiredService<ITypeDefinitionService>();

        return await typeDefinitionService.GenerateTypeDefinitionsAsync(typeDefinitionContext);
    }
}
