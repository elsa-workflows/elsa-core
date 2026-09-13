using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dsl.ElsaScript.IntegrationTests;

/// <summary>
/// Integration tests for the ElsaScript parser.
/// </summary>
public class ParserTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly IElsaScriptParser _parser;

    public ParserTests()
    {
        _services = new TestApplicationBuilder(TestContext.Current!.Output.StandardOutput)
            .ConfigureElsa(elsa => elsa.UseElsaScript())
            .Build();

        _parser = _services.GetRequiredService<IElsaScriptParser>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (_services is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    [DisplayName("Parser can parse a simple workflow definition")]
    public async Task Parse_WithSimpleWorkflowDefinition_ShouldReturnWorkflowWithCorrectStructure()
    {
        // Arrange
        var source = @"
use Elsa.Activities.Console;
use expressions js;

workflow HelloWorld {
  WriteLine(""Hello World"");
  WriteLine(""Great to meet you!"");
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        await Assert.That(workflow.Id).IsEqualTo("HelloWorld");


        // Global use statements
        await Assert.That(program.GlobalUseStatements.Count).IsEqualTo(2);


        // No workflow-level use statements (all are global)
        await Assert.That(workflow.UseStatements).IsEmpty();


        await Assert.That(workflow.Body.Count).IsEqualTo(2);

    }

    [Test]
    [DisplayName("Parser can parse variable declarations")]
    public async Task Parse_WithVariableDeclarations_ShouldReturnWorkflowWithAllVariableNodes()
    {
        // Arrange
        var source = @"
use expressions js;

workflow VariableTest {
  var greeting = ""Hello"";
  var count = 42;
  const pi = 3.14;
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        await Assert.That(workflow.Id).IsEqualTo("VariableTest");

        await Assert.That(workflow.Body.Count).IsEqualTo(3);


        var varDeclValue = workflow.Body[0];


        await Assert.That(varDeclValue).IsOfType(typeof(Ast.VariableDeclarationNode));


        var varDecl = (Ast.VariableDeclarationNode)varDeclValue!;
        await Assert.That(varDecl.Kind).IsEqualTo(Ast.VariableKind.Var);

        await Assert.That(varDecl.Name).IsEqualTo("greeting");

    }

    [Test]
    [DisplayName("Parser can parse activity invocations with named arguments")]
    public async Task Parse_WithActivityInvocationWithNamedArguments_ShouldReturnActivityNodeWithCorrectArguments()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ActivityTest {
  WriteLine(Text: ""Hello World"");
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        await Assert.That(workflow.Body).HasSingleItem();


        var activityValue = workflow.Body[0];


        await Assert.That(activityValue).IsOfType(typeof(Ast.ActivityInvocationNode));


        var activity = (Ast.ActivityInvocationNode)activityValue!;
        await Assert.That(activity.ActivityName).IsEqualTo("WriteLine");

        await Assert.That(activity.Arguments).HasSingleItem();

        await Assert.That(activity.Arguments[0].Name).IsEqualTo("Text");

    }

    [Test]
    [DisplayName("Parser can parse listen statements")]
    public async Task Parse_WithListenStatement_ShouldReturnWorkflowWithListenNode()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ListenTest {
  listen HttpEndpoint(""/test"");
  WriteLine(""Triggered!"");
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        await Assert.That(workflow.Body.Count).IsEqualTo(2);


        var listenValue = workflow.Body[0];


        await Assert.That(listenValue).IsOfType(typeof(Ast.ListenNode));


        var listen = (Ast.ListenNode)listenValue!;
        await Assert.That(listen.Activity.ActivityName).IsEqualTo("HttpEndpoint");

    }

    [Test]
    [DisplayName("Parser can parse workflow without workflow keyword")]
    public async Task Parse_WithoutWorkflowKeyword_ShouldReturnWorkflowWithCorrectStructure()
    {
        // Arrange
        var source = @"WriteLine(""Hello World""); WriteLine(""Great to meet you!"");";

        // Act
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        await Assert.That(workflow.Body.Count).IsEqualTo(2);

    }

    [Test]
    [DisplayName("Parser can parse complex workflow with variables, listen statements, and ElsaScript expressions")]
    public async Task Parse_WithComplexWorkflow_ShouldReturnWorkflowWithCorrectStructure()
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
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        await Assert.That(workflow.Id).IsEqualTo("HelloWorldHttpDsl");


        // Verify global use statement
        await Assert.That(program.GlobalUseStatements).HasSingleItem();

        var useStatement = program.GlobalUseStatements[0];
        await Assert.That(useStatement.Type).IsEqualTo(Ast.UseType.Expressions);

        await Assert.That(useStatement.Value).IsEqualTo("js");


        // No workflow-level use statements
        await Assert.That(workflow.UseStatements).IsEmpty();


        // Verify body contains: var declaration, listen statement, WriteLine, WriteHttpResponse
        await Assert.That(workflow.Body.Count).IsEqualTo(4);


        // Verify variable declaration
        var varDeclValue = workflow.Body[0];
        await Assert.That(varDeclValue).IsOfType(typeof(Ast.VariableDeclarationNode));
        var varDecl = (Ast.VariableDeclarationNode)varDeclValue!;
        await Assert.That(varDecl.Name).IsEqualTo("message");

        await Assert.That(varDecl.Kind).IsEqualTo(Ast.VariableKind.Var);


        // Verify listen statement
        var listenNodeValue = workflow.Body[1];
        await Assert.That(listenNodeValue).IsOfType(typeof(Ast.ListenNode));
        var listenNode = (Ast.ListenNode)listenNodeValue!;
        await Assert.That(listenNode.Activity.ActivityName).IsEqualTo("HttpEndpoint");

        await Assert.That(listenNode.Activity.Arguments).HasSingleItem();


        // Verify WriteLine with ElsaScript expression
        var writeLineNodeValue = workflow.Body[2];
        await Assert.That(writeLineNodeValue).IsOfType(typeof(Ast.ActivityInvocationNode));
        var writeLineNode = (Ast.ActivityInvocationNode)writeLineNodeValue!;
        await Assert.That(writeLineNode.ActivityName).IsEqualTo("WriteLine");

        await Assert.That(writeLineNode.Arguments).HasSingleItem();

        var writeLineExprValue = writeLineNode.Arguments[0].Value;

        await Assert.That(writeLineExprValue).IsOfType(typeof(Ast.ElsaExpressionNode));

        var writeLineExpr = (Ast.ElsaExpressionNode)writeLineExprValue!;
        await Assert.That(writeLineExpr.Language).IsEqualTo("js");


        // Verify WriteHttpResponse with variable reference
        var writeHttpNodeValue = workflow.Body[3];
        await Assert.That(writeHttpNodeValue).IsOfType(typeof(Ast.ActivityInvocationNode));
        var writeHttpNode = (Ast.ActivityInvocationNode)writeHttpNodeValue!;
        await Assert.That(writeHttpNode.ActivityName).IsEqualTo("WriteHttpResponse");

        await Assert.That(writeHttpNode.Arguments).HasSingleItem();

        var writeHttpArgValue = writeHttpNode.Arguments[0].Value;

        await Assert.That(writeHttpArgValue).IsOfType(typeof(Ast.IdentifierNode));

        var writeHttpArg = (Ast.IdentifierNode)writeHttpArgValue!;
        await Assert.That(writeHttpArg.Name).IsEqualTo("message");

    }

    [Test]
    [DisplayName("Parser can parse for loop with 'to' keyword (exclusive)")]
    public async Task Parse_WithForLoopExclusive_ShouldReturnWorkflowWithForNode()
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
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        await Assert.That(workflow.Id).IsEqualTo("ForLoopTest");

        await Assert.That(workflow.Body).HasSingleItem();


        var forNodeValue = workflow.Body[0];


        await Assert.That(forNodeValue).IsOfType(typeof(Ast.ForNode));


        var forNode = (Ast.ForNode)forNodeValue!;
        await Assert.That(forNode.DeclaresVariable).IsTrue(); // var i
        await Assert.That(forNode.VariableName).IsEqualTo("i");

        await Assert.That(forNode.IsInclusive).IsFalse();


        var startLiteralValue = forNode.Start;


        await Assert.That(startLiteralValue).IsOfType(typeof(Ast.LiteralNode));


        var startLiteral = (Ast.LiteralNode)startLiteralValue!;
        // Numbers are parsed as decimals by the parser
        await Assert.That(Convert.ToDecimal(startLiteral.Value!)).IsEqualTo(0m);


        var endLiteralValue = forNode.End;


        await Assert.That(endLiteralValue).IsOfType(typeof(Ast.LiteralNode));


        var endLiteral = (Ast.LiteralNode)endLiteralValue!;
        await Assert.That(Convert.ToDecimal(endLiteral.Value!)).IsEqualTo(10m);


        var stepLiteralValue = forNode.Step;


        await Assert.That(stepLiteralValue).IsOfType(typeof(Ast.LiteralNode));


        var stepLiteral = (Ast.LiteralNode)stepLiteralValue!;
        await Assert.That(Convert.ToDecimal(stepLiteral.Value!)).IsEqualTo(1m);

    }

    [Test]
    [DisplayName("Parser can parse for loop with 'through' keyword (inclusive)")]
    public async Task Parse_WithForLoopInclusive_ShouldReturnWorkflowWithForNode()
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
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        await Assert.That(workflow.Id).IsEqualTo("ForLoopInclusiveTest");

        await Assert.That(workflow.Body).HasSingleItem();


        var forNodeValue = workflow.Body[0];


        await Assert.That(forNodeValue).IsOfType(typeof(Ast.ForNode));


        var forNode = (Ast.ForNode)forNodeValue!;
        await Assert.That(forNode.DeclaresVariable).IsTrue(); // var i
        await Assert.That(forNode.VariableName).IsEqualTo("i");

        await Assert.That(forNode.IsInclusive).IsTrue();

    }

    [Test]
    [DisplayName("Parser can parse workflow with metadata")]
    public async Task Parse_WithWorkflowMetadata_ShouldReturnWorkflowWithCorrectMetadata()
    {
        // Arrange
        var source = @"
use Elsa.Activities.Console;

workflow HelloWorldDsl(
  DisplayName: ""Hello World DSL"",
  Description: ""Demonstrates ElsaScript with metadata"",
  DefinitionId: ""hello-world-dsl"",
  DefinitionVersionId: ""hello-world-dsl-v1"",
  Version: 1,
  UsableAsActivity: true
) {
  use expressions js;

  WriteLine(""Hello World from Elsa DSL!"");
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        await Assert.That(workflow.Id).IsEqualTo("HelloWorldDsl");


        // Check metadata
        await Assert.That((string)workflow.Metadata["DisplayName"]).IsEqualTo("Hello World DSL");

        await Assert.That((string)workflow.Metadata["Description"]).IsEqualTo("Demonstrates ElsaScript with metadata");

        await Assert.That((string)workflow.Metadata["DefinitionId"]).IsEqualTo("hello-world-dsl");

        await Assert.That((string)workflow.Metadata["DefinitionVersionId"]).IsEqualTo("hello-world-dsl-v1");

        await Assert.That(Convert.ToInt64(workflow.Metadata["Version"])).IsEqualTo(1L);

        await Assert.That(Convert.ToBoolean(workflow.Metadata["UsableAsActivity"])).IsTrue();


        // Check use statements (workflow-level only, global handled separately)
        await Assert.That(workflow.UseStatements).HasSingleItem();

        await Assert.That(workflow.UseStatements[0].Type).IsEqualTo(Ast.UseType.Expressions);

        await Assert.That(workflow.UseStatements[0].Value).IsEqualTo("js");


        // Check body
        await Assert.That(workflow.Body).HasSingleItem();

    }

    [Test]
    [DisplayName("Parser can parse empty flowchart")]
    public async Task Parse_WithEmptyFlowchart_ShouldReturnFlowchartNode()
    {
        // Arrange
        var source = @"
workflow FlowchartTest {
  flowchart {
  }
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        await Assert.That(workflow.Id).IsEqualTo("FlowchartTest");

        await Assert.That(workflow.Body).HasSingleItem();


        await Assert.That(workflow.Body[0]).IsOfType(typeof(Ast.FlowchartNode));

    }

    [Test]
    [DisplayName("Parser can parse flowchart with node and connection")]
    public async Task Parse_WithFlowchartNodeAndConnection_ShouldReturnCorrectStructure()
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
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        await Assert.That(program.Workflows).HasSingleItem();


        var workflow = program.Workflows[0];
        var flowchartValue = workflow.Body[0];
        await Assert.That(flowchartValue).IsOfType(typeof(Ast.FlowchartNode));
        var flowchart = (Ast.FlowchartNode)flowchartValue!;

        // Check entry point
        await Assert.That(flowchart.EntryPoint).IsEqualTo("Start");


        // Check nodes
        await Assert.That(flowchart.Activities.Count).IsEqualTo(2);


        // Check connections
        await Assert.That(flowchart.Connections).HasSingleItem();

        await Assert.That(flowchart.Connections[0].Source).IsEqualTo("Start");

        await Assert.That(flowchart.Connections[0].Target).IsEqualTo("End");

    }

    [Test]
    [DisplayName("Parser can parse flowchart with block node")]
    public async Task Parse_WithFlowchartBlockNode_ShouldReturnBlockStatement()
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
        var program = _parser.Parse(source);

        // Assert
        program = (await Assert.That(program).IsNotNull())!;

        var workflow = program.Workflows[0];
        var flowchartValue = workflow.Body[0];
        await Assert.That(flowchartValue).IsOfType(typeof(Ast.FlowchartNode));
        var flowchart = (Ast.FlowchartNode)flowchartValue!;

        await Assert.That(flowchart.Activities).HasSingleItem();

        await Assert.That(flowchart.Activities[0].Label).IsEqualTo("Start");


        // The block should be a BlockNode with 2 statements
        var blockNodeValue = flowchart.Activities[0].Activity;
        await Assert.That(blockNodeValue).IsOfType(typeof(Ast.BlockNode));
        var blockNode = (Ast.BlockNode)blockNodeValue!;
        await Assert.That(blockNode.Statements.Count).IsEqualTo(2);

    }

}
