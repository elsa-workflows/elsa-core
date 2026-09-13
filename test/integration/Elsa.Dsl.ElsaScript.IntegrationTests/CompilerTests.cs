using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dsl.ElsaScript.IntegrationTests;

/// <summary>
/// Integration tests for the ElsaScript compiler.
/// </summary>
public class CompilerTests : IAsyncDisposable
{
    private readonly IElsaScriptCompiler _compiler;
    private readonly IServiceProvider _services;

    public CompilerTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureElsa(elsa =>
            {
                elsa.UseElsaScript();
                elsa.AddActivitiesFrom<HttpEndpoint>();
            })
            .Build();

        _compiler = _services.GetRequiredService<IElsaScriptCompiler>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_services is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    [DisplayName("Compiler can compile a simple workflow from source")]
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
        workflow = (await Assert.That(workflow).IsNotNull())!;

        await Assert.That(workflow.Name).IsEqualTo("HelloWorld");

        await Assert.That(workflow.Root).IsNotNull();

    }

    [Test]
    [DisplayName("Compiler can compile workflow with variable declarations")]
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
        workflow = (await Assert.That(workflow).IsNotNull())!;

        await Assert.That(workflow.Name).IsEqualTo("VariableTest");

        await Assert.That(workflow.Variables.Count).IsEqualTo(3);


        await Assert.That(workflow.Variables).Contains(v => v.Name == "message");

        await Assert.That(workflow.Variables).Contains(v => v.Name == "count");

        await Assert.That(workflow.Variables).Contains(v => v.Name == "pi");

    }

    [Test]
    [DisplayName("Compiler can compile workflow without workflow keyword")]
    public async Task CompileAsync_WithoutWorkflowKeyword_ShouldCreateWorkflow()
    {
        // Arrange
        var source = @"WriteLine(""Hello World"");";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert
        workflow = (await Assert.That(workflow).IsNotNull())!;

        await Assert.That(workflow.Root).IsNotNull();

    }

    [Test]
    [DisplayName("Compiler can compile complex workflow with variables, listen statements, and expressions")]
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
        workflow = (await Assert.That(workflow).IsNotNull())!;

        await Assert.That(workflow.Name).IsEqualTo("HelloWorldHttpDsl");


        // Verify workflow has the message variable
        await Assert.That(workflow.Variables).HasSingleItem();

        var messageVar = workflow.Variables.First();
        await Assert.That(messageVar.Name).IsEqualTo("message");

        await Assert.That(messageVar.Value).IsEqualTo("Hello World from DSL via Expressions!");


        // Verify root is a Sequence with 3 activities (HttpEndpoint, WriteLine, WriteHttpResponse)
        var sequenceValue = workflow.Root;
        await Assert.That(sequenceValue).IsOfType(typeof(Sequence));
        var sequence = (Sequence)sequenceValue!;
        await Assert.That(sequence.Activities.Count).IsEqualTo(3);


        // Verify HttpEndpoint activity (from listen statement)
        var httpEndpoint = sequence.Activities.ElementAt(0);
        await Assert.That(httpEndpoint.Type).IsEqualTo("Elsa.HttpEndpoint");


        // Verify HttpEndpoint can start workflow (CanStartWorkflow property should be true)
        var canStartWorkflowProp = httpEndpoint.GetType().GetProperty("CanStartWorkflow");
        canStartWorkflowProp = (await Assert.That(canStartWorkflowProp).IsNotNull())!;

        var canStartWorkflow = (bool)canStartWorkflowProp.GetValue(httpEndpoint)!;
        await Assert.That(canStartWorkflow).IsTrue();


        // Verify WriteLine activity with JavaScript expression
        var writeLine = sequence.Activities.ElementAt(1);
        await Assert.That(writeLine.Type).IsEqualTo("Elsa.WriteLine");


        var textProp = writeLine.GetType().GetProperty("Text");
        textProp = (await Assert.That(textProp).IsNotNull())!;

        var textInputValue = textProp.GetValue(writeLine);

        await Assert.That(textInputValue).IsOfType(typeof(Input<string>));

        var textInput = (Input<string>)textInputValue!;

        // Verify it's a JavaScript expression
        var expression = textInput.Expression;
        expression = (await Assert.That(expression).IsNotNull())!;

        await Assert.That(expression.Type).IsEqualTo("JavaScript");

        await Assert.That(expression.Value?.ToString()).Contains("Message:", StringComparison.CurrentCulture);

        await Assert.That(expression.Value?.ToString()).Contains("message", StringComparison.CurrentCulture);


        // Verify WriteHttpResponse activity with variable reference
        var writeHttpResponse = sequence.Activities.ElementAt(2);
        await Assert.That(writeHttpResponse.Type).IsEqualTo("Elsa.WriteHttpResponse");


        var contentProp = writeHttpResponse.GetType().GetProperty("Content");
        contentProp = (await Assert.That(contentProp).IsNotNull())!;

        var contentInputValue = contentProp.GetValue(writeHttpResponse);

        await Assert.That(contentInputValue).IsOfType(typeof(Input<object>));

        var contentInput = (Input<object>)contentInputValue!;

        // Verify it references the message variable
        var memoryBlockReference = contentInput.MemoryBlockReference();
        memoryBlockReference = (await Assert.That(memoryBlockReference).IsNotNull())!;

        await Assert.That(memoryBlockReference.Id).IsEqualTo(messageVar.Id);

    }

    [Test]
    [DisplayName("Compiler can compile for loop with 'to' keyword (exclusive)")]
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
        workflow = (await Assert.That(workflow).IsNotNull())!;

        await Assert.That(workflow.Name).IsEqualTo("ForLoopTest");


        // Verify the For activity is created
        var forActivityValue = workflow.Root;
        await Assert.That(forActivityValue).IsOfType(typeof(For));
        var forActivity = (For)forActivityValue!;
        await Assert.That(forActivity).IsNotNull();


        // Verify Start, End, Step values
        await Assert.That(forActivity.Start).IsNotNull();

        await Assert.That(forActivity.End).IsNotNull();

        await Assert.That(forActivity.Step).IsNotNull();


        // Verify OuterBoundInclusive is false (exclusive 'to')
        var outerBoundInput = (await Assert.That(forActivity.OuterBoundInclusive).IsNotNull())!;
        var outerBoundExpr = outerBoundInput.Expression;
        outerBoundExpr = (await Assert.That(outerBoundExpr).IsNotNull())!;

        await Assert.That((bool?)outerBoundExpr.Value).IsFalse();


        // Verify loop variable exists
        await Assert.That(workflow.Variables).HasSingleItem();

        var loopVar = workflow.Variables.First();
        await Assert.That(loopVar.Name).IsEqualTo("i");


        // Verify body exists
        await Assert.That(forActivity.Body).IsNotNull();

    }

    [Test]
    [DisplayName("Compiler can compile for loop with 'through' keyword (inclusive)")]
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
        workflow = (await Assert.That(workflow).IsNotNull())!;

        await Assert.That(workflow.Name).IsEqualTo("ForLoopInclusiveTest");


        // Verify the For activity is created
        var forActivityValue = workflow.Root;
        await Assert.That(forActivityValue).IsOfType(typeof(For));
        var forActivity = (For)forActivityValue!;
        await Assert.That(forActivity).IsNotNull();


        // Verify OuterBoundInclusive is true (inclusive 'through')
        var outerBoundInput = (await Assert.That(forActivity.OuterBoundInclusive).IsNotNull())!;
        var outerBoundExpr = outerBoundInput.Expression;
        outerBoundExpr = (await Assert.That(outerBoundExpr).IsNotNull())!;

        await Assert.That((bool?)outerBoundExpr.Value).IsTrue();


        // Verify loop variable exists
        await Assert.That(workflow.Variables).HasSingleItem();

        var loopVar = workflow.Variables.First();
        await Assert.That(loopVar.Name).IsEqualTo("i");

    }

    [Test]
    [DisplayName("Compiler can compile workflow with metadata")]
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
        workflow = (await Assert.That(workflow).IsNotNull())!;


        // Check identity
        await Assert.That(workflow.Identity.DefinitionId).IsEqualTo("hello-world-dsl");

        await Assert.That(workflow.Identity.Version).IsEqualTo(2);

        await Assert.That(workflow.Identity.Id).IsEqualTo("hello-world-dsl-v1");


        // Check metadata
        await Assert.That(workflow.WorkflowMetadata.Name).IsEqualTo("Hello World DSL");

        await Assert.That(workflow.WorkflowMetadata.Description).IsEqualTo("Demonstrates ElsaScript with metadata");


        // Check options
        await Assert.That(workflow.Options.UsableAsActivity).IsTrue();


        // Check root activity
        await Assert.That(workflow.Root).IsNotNull();

    }

    [Test]
    [DisplayName("Compiler can compile empty flowchart")]
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
        workflow = (await Assert.That(workflow).IsNotNull())!;

        await Assert.That(workflow.Root).IsNotNull();


        await Assert.That(workflow.Root).IsOfType(typeof(Workflows.Activities.Flowchart.Activities.Flowchart));

    }

    [Test]
    [DisplayName("Compiler can compile flowchart with node and connection")]
    public async Task CompileAsync_WithFlowchartNodeAndConnection_ShouldCreateFlowchartWithCorrectStructure()
    {
        // Arrange
        var source = @"
workflow FlowchartTest {
  flowchart {
    entry Start;
    Start: WriteLine();
    End: WriteLine();
    Start -> End;
  }
}";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert
        workflow = (await Assert.That(workflow).IsNotNull())!;

        await Assert.That(workflow.Root).IsNotNull();


        var flowchartValue = workflow.Root;


        await Assert.That(flowchartValue).IsOfType(typeof(Workflows.Activities.Flowchart.Activities.Flowchart));


        var flowchart = (Workflows.Activities.Flowchart.Activities.Flowchart)flowchartValue!;

        // Check that flowchart has 2 activities
        await Assert.That(flowchart.Activities.Count).IsEqualTo(2);


        // Check connections
        await Assert.That(flowchart.Connections).HasSingleItem();


        // Check entry point is set
        await Assert.That(flowchart.Start).IsNotNull();

    }

    [Test]
    [DisplayName("Compiler can compile flowchart with block node")]
    public async Task CompileAsync_WithFlowchartBlockNode_ShouldCreateSequenceActivity()
    {
        // Arrange
        var source = @"
workflow FlowchartWithBlock {
  flowchart {
    entry Start;

    Start: {
      WriteLine(""First"");
      WriteLine(""Second"");
    }
  }
}";

        // Act
        var workflow = await _compiler.CompileAsync(source);

        // Assert
        workflow = (await Assert.That(workflow).IsNotNull())!;

        await Assert.That(workflow.Root).IsNotNull();


        var flowchartValue = workflow.Root;


        await Assert.That(flowchartValue).IsOfType(typeof(Workflows.Activities.Flowchart.Activities.Flowchart));


        var flowchart = (Workflows.Activities.Flowchart.Activities.Flowchart)flowchartValue!;

        // Should have one activity
        await Assert.That(flowchart.Activities).HasSingleItem();


        // The activity should be a Sequence (from the block)
        var sequenceActivityValue = flowchart.Activities.First();
        await Assert.That(sequenceActivityValue).IsOfType(typeof(Sequence));
        var sequenceActivity = (Sequence)sequenceActivityValue!;

        // Sequence should have 2 activities
        await Assert.That(sequenceActivity.Activities.Count).IsEqualTo(2);

    }

    [Test]
    [DisplayName("Compiler resets default expression language between compilations")]
    public async Task CompileAsync_WithSecondWorkflowAfterLiquid_ShouldResetToJavaScript()
    {
        // Arrange - First workflow sets Liquid as default
        var firstSource = @"
use expressions liquid;

workflow FirstWorkflow {
  WriteLine(liquid => ""{{ 'test' }}"");
}";

        // Second workflow with no 'use expressions' directive
        var secondSource = @"
workflow SecondWorkflow {
  WriteLine(js => ""test"");
}";

        // Act - Compile first workflow (sets default to Liquid)
        var firstWorkflow = await _compiler.CompileAsync(firstSource);

        // Act - Compile second workflow (should reset to JavaScript)
        var secondWorkflow = await _compiler.CompileAsync(secondSource);

        // Assert - Verify first workflow used Liquid
        firstWorkflow = (await Assert.That(firstWorkflow).IsNotNull())!;

        var firstWriteLine = firstWorkflow.Root;
        var firstTextProp = firstWriteLine.GetType().GetProperty("Text");
        var firstTextInputValue = firstTextProp!.GetValue(firstWriteLine);
        await Assert.That(firstTextInputValue).IsOfType(typeof(Input<string>));
        var firstTextInput = (Input<string>)firstTextInputValue!;
        await Assert.That(firstTextInput.Expression?.Type).IsEqualTo("Liquid");


        // Assert - Verify second workflow uses JavaScript (not leaked Liquid)
        secondWorkflow = (await Assert.That(secondWorkflow).IsNotNull())!;

        var secondWriteLine = secondWorkflow.Root;
        var secondTextProp = secondWriteLine.GetType().GetProperty("Text");
        var secondTextInputValue = secondTextProp!.GetValue(secondWriteLine);
        await Assert.That(secondTextInputValue).IsOfType(typeof(Input<string>));
        var secondTextInput = (Input<string>)secondTextInputValue!;
        await Assert.That(secondTextInput.Expression?.Type).IsEqualTo("JavaScript");

    }
}
