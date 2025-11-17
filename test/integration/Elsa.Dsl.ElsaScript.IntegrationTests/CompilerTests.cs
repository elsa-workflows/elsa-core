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

workflow HelloWorld {
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

workflow VariableTest {
  var message = ""Hello from variable"";
  var count = 42;
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

workflow HelloWorldHttpDsl {
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
        Assert.Contains("Message:", expression.Value?.ToString());
        Assert.Contains("message", expression.Value?.ToString());

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

    [Fact(DisplayName = "Compiler can compile for loop with 'to' keyword (exclusive)")]
    public async Task CompileAsync_WithForLoopExclusive_ShouldCreateForActivity()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ForLoopTest {
  for (var i = 0 to 10 step 1)
  {
    WriteLine(js => `Step: ${i}`)
  }
}";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("ForLoopTest", workflow.Name);

        // Verify the For activity is created
        var forActivity = Assert.IsType<For>(workflow.Root);
        Assert.NotNull(forActivity);

        // Verify Start, End, Step values
        Assert.NotNull(forActivity.Start);
        Assert.NotNull(forActivity.End);
        Assert.NotNull(forActivity.Step);

        // Verify OuterBoundInclusive is false (exclusive 'to')
        Assert.NotNull(forActivity.OuterBoundInclusive);
        var outerBoundInput = forActivity.OuterBoundInclusive;
        var outerBoundExpr = outerBoundInput.Expression;
        Assert.NotNull(outerBoundExpr);
        Assert.False((bool?)outerBoundExpr.Value);

        // Verify loop variable exists
        Assert.Single(workflow.Variables);
        var loopVar = workflow.Variables.First();
        Assert.Equal("i", loopVar.Name);

        // Verify body exists
        Assert.NotNull(forActivity.Body);
    }

    [Fact(DisplayName = "Compiler can compile for loop with 'through' keyword (inclusive)")]
    public async Task CompileAsync_WithForLoopInclusive_ShouldCreateForActivity()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ForLoopInclusiveTest {
  for (var i = 0 through 10 step 1)
  {
    WriteLine(js => `Step: ${i}`)
  }
}";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("ForLoopInclusiveTest", workflow.Name);

        // Verify the For activity is created
        var forActivity = Assert.IsType<For>(workflow.Root);
        Assert.NotNull(forActivity);

        // Verify OuterBoundInclusive is true (inclusive 'through')
        Assert.NotNull(forActivity.OuterBoundInclusive);
        var outerBoundInput = forActivity.OuterBoundInclusive;
        var outerBoundExpr = outerBoundInput.Expression;
        Assert.NotNull(outerBoundExpr);
        Assert.True((bool?)outerBoundExpr.Value);

        // Verify loop variable exists
        Assert.Single(workflow.Variables);
        var loopVar = workflow.Variables.First();
        Assert.Equal("i", loopVar.Name);
    }

    [Fact(DisplayName = "Compiler can compile workflow with metadata")]
    public async Task CompileAsync_WithWorkflowMetadata_ShouldCreateWorkflowWithCorrectMetadata()
    {
        // Arrange
        var source = @"
use Elsa.Activities.Console;

workflow HelloWorldDsl(
  DisplayName: ""Hello World DSL"",
  Description: ""Demonstrates ElsaScript with metadata"",
  DefinitionId: ""hello-world-dsl"",
  DefinitionVersionId: ""hello-world-dsl-v1"",
  Version: 2,
  UsableAsActivity: true
) {
  use expressions js;

  WriteLine(""Hello World from Elsa DSL!"");
}";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert
        Assert.NotNull(workflow);

        // Check identity
        Assert.Equal("hello-world-dsl", workflow.Identity.DefinitionId);
        Assert.Equal(2, workflow.Identity.Version);
        Assert.Equal("hello-world-dsl-v1", workflow.Identity.Id);

        // Check metadata
        Assert.Equal("Hello World DSL", workflow.WorkflowMetadata.Name);
        Assert.Equal("Demonstrates ElsaScript with metadata", workflow.WorkflowMetadata.Description);

        // Check options
        Assert.True(workflow.Options.UsableAsActivity);

        // Check root activity
        Assert.NotNull(workflow.Root);
    }

    [Fact(DisplayName = "Compiler can compile empty flowchart")]
    public async Task CompileAsync_WithEmptyFlowchart_ShouldCreateFlowchartActivity()
    {
        // Arrange
        var source = @"
workflow FlowchartTest {
  flowchart {
  }
}";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert
        Assert.NotNull(workflow);
        Assert.NotNull(workflow.Root);

        var flowchart = workflow.Root as Workflows.Activities.Flowchart.Activities.Flowchart;
        Assert.NotNull(flowchart);
    }
}

