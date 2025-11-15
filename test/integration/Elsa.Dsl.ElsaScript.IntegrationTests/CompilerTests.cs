using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Dsl.ElsaScript.IntegrationTests;

/// <summary>
/// Integration tests for the ElsaScript compiler.
/// Note: These tests demonstrate the DSL concept but some tests are skipped
/// due to challenges with dynamic activity instantiation that need further work.
/// </summary>
public class CompilerTests
{
    private readonly IElsaScriptCompiler _compiler;

    public CompilerTests(ITestOutputHelper testOutputHelper)
    {
        var services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa =>
            {
                elsa.UseElsaScript();
                elsa.AddActivitiesFrom<HttpEndpoint>();
            })
            .Build();

        _compiler = services.GetRequiredService<IElsaScriptCompiler>();
    }

    [Fact(DisplayName = "Compiler can compile a simple workflow from source")]
    public async Task CompileAsync_WithSimpleWorkflowSource_ShouldCreateWorkflowWithCorrectName()
    {
        // Arrange
        var source = @"
use Elsa.Activities.Console;
use expressions js;

workflow ""HelloWorld"" {
  WriteLine(""Hello World"");
}";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert - verify the workflow is correctly compiled
        Assert.NotNull(workflow);
        Assert.Equal("HelloWorld", workflow.Name);
        Assert.NotNull(workflow.Root);
    }

    [Fact(DisplayName = "Compiler can compile workflow with variable declarations")]
    public async Task CompileAsync_WithVariableDeclarations_ShouldCreateWorkflowWithAllVariables()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ""VariableTest"" {
  var message = ""Hello from variable"";
  let count = 42;
  const pi = 3.14;
}";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert - verify variables are compiled correctly
        Assert.NotNull(workflow);
        Assert.Equal("VariableTest", workflow.Name);
        Assert.Equal(3, workflow.Variables.Count);

        Assert.Contains(workflow.Variables, v => v.Name == "message");
        Assert.Contains(workflow.Variables, v => v.Name == "count");
        Assert.Contains(workflow.Variables, v => v.Name == "pi");
    }

    [Fact(DisplayName = "Compiler can compile workflow without workflow keyword")]
    public async Task CompileAsync_WithoutWorkflowKeyword_ShouldCreateWorkflow()
    {
        // Arrange
        var source = @"WriteLine(""Hello World"");";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert
        Assert.NotNull(workflow);
        Assert.NotNull(workflow.Root);
    }

    [Fact(DisplayName = "Compiler can compile complex workflow with variables, listen statements, and expressions")]
    public async Task CompileAsync_WithComplexWorkflow_ShouldCreateWorkflowWithCorrectStructure()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ""HelloWorldHttpDsl"" {
    var message = ""Hello World from DSL via Expressions!"";
    listen HttpEndpoint(""/hello-world-dsl"");
    WriteLine(js => `Message: ${message}`);
    WriteHttpResponse(message);
}";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("HelloWorldHttpDsl", workflow.Name);

        // Verify workflow has the message variable
        Assert.Single(workflow.Variables);
        var messageVar = workflow.Variables.First();
        Assert.Equal("message", messageVar.Name);
        Assert.Equal("Hello World from DSL via Expressions!", messageVar.Value);

        // Verify root is a Sequence with 3 activities (HttpEndpoint, WriteLine, WriteHttpResponse)
        var sequence = Assert.IsType<Sequence>(workflow.Root);
        Assert.Equal(3, sequence.Activities.Count);

        // Verify HttpEndpoint activity (from listen statement)
        var httpEndpoint = sequence.Activities.ElementAt(0);
        Assert.Equal("Elsa.HttpEndpoint", httpEndpoint.Type);

        // Verify HttpEndpoint can start workflow (CanStartWorkflow property should be true)
        var canStartWorkflowProp = httpEndpoint.GetType().GetProperty("CanStartWorkflow");
        Assert.NotNull(canStartWorkflowProp);
        var canStartWorkflow = (bool)canStartWorkflowProp!.GetValue(httpEndpoint)!;
        Assert.True(canStartWorkflow);

        // Verify WriteLine activity with JavaScript expression
        var writeLine = sequence.Activities.ElementAt(1);
        Assert.Equal("Elsa.WriteLine", writeLine.Type);

        var textProp = writeLine.GetType().GetProperty("Text");
        Assert.NotNull(textProp);
        var textInput = textProp!.GetValue(writeLine) as Input<string>;
        Assert.NotNull(textInput);

        // Verify it's a JavaScript expression
        var expression = textInput!.Expression;
        Assert.NotNull(expression);
        Assert.Equal("JavaScript", expression!.Type);
        Assert.Contains("Message:", expression.Value.ToString()!);
        Assert.Contains("message", expression.Value.ToString()!);

        // Verify WriteHttpResponse activity with variable reference
        var writeHttpResponse = sequence.Activities.ElementAt(2);
        Assert.Equal("Elsa.WriteHttpResponse", writeHttpResponse.Type);

        var contentProp = writeHttpResponse.GetType().GetProperty("Content");
        Assert.NotNull(contentProp);
        var contentInput = contentProp!.GetValue(writeHttpResponse) as Input<object>;
        Assert.NotNull(contentInput);

        // Verify it references the message variable
        var memoryBlockReference = contentInput!.MemoryBlockReference();
        Assert.NotNull(memoryBlockReference);
        Assert.Equal(messageVar.Id, memoryBlockReference!.Id);
    }
}

