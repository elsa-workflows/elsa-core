using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Dsl.ElsaScript.IntegrationTests;

/// <summary>
/// Integration tests for the ElsaScript parser.
/// </summary>
public class ParserTests
{
    private readonly IServiceProvider _services;
    private readonly IElsaScriptParser _parser;

    public ParserTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseElsaScript())
            .Build();

        _parser = _services.GetRequiredService<IElsaScriptParser>();
    }

    [Fact(DisplayName = "Parser can parse a simple workflow definition")]
    public void Parse_WithSimpleWorkflowDefinition_ShouldReturnWorkflowWithCorrectStructure()
    {
        // Arrange
        var source = @"
use Elsa.Activities.Console;
use expressions js;

workflow ""HelloWorld"" {
  WriteLine(""Hello World"");
  WriteLine(""Great to meet you!"");
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        Assert.NotNull(program);
        Assert.Single(program.Workflows);

        var workflow = program.Workflows[0];
        Assert.Equal("HelloWorld", workflow.Id);

        // Global use statements
        Assert.Equal(2, program.GlobalUseStatements.Count);

        // No workflow-level use statements (all are global)
        Assert.Empty(workflow.UseStatements);

        Assert.Equal(2, workflow.Body.Count);
    }

    [Fact(DisplayName = "Parser can parse variable declarations")]
    public void Parse_WithVariableDeclarations_ShouldReturnWorkflowWithAllVariableNodes()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ""VariableTest"" {
  var greeting = ""Hello"";
  var count = 42;
  const pi = 3.14;
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        Assert.NotNull(program);
        Assert.Single(program.Workflows);

        var workflow = program.Workflows[0];
        Assert.Equal("VariableTest", workflow.Id);
        Assert.Equal(3, workflow.Body.Count);

        var varDecl = workflow.Body[0] as Ast.VariableDeclarationNode;
        Assert.NotNull(varDecl);
        Assert.Equal(Ast.VariableKind.Var, varDecl!.Kind);
        Assert.Equal("greeting", varDecl.Name);
    }

    [Fact(DisplayName = "Parser can parse activity invocations with named arguments")]
    public void Parse_WithActivityInvocationWithNamedArguments_ShouldReturnActivityNodeWithCorrectArguments()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ""ActivityTest"" {
  WriteLine(Text: ""Hello World"");
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        Assert.NotNull(program);
        Assert.Single(program.Workflows);

        var workflow = program.Workflows[0];
        Assert.Single(workflow.Body);

        var activity = workflow.Body[0] as Ast.ActivityInvocationNode;
        Assert.NotNull(activity);
        Assert.Equal("WriteLine", activity!.ActivityName);
        Assert.Single(activity.Arguments);
        Assert.Equal("Text", activity.Arguments[0].Name);
    }

    [Fact(DisplayName = "Parser can parse listen statements")]
    public void Parse_WithListenStatement_ShouldReturnWorkflowWithListenNode()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ""ListenTest"" {
  listen HttpEndpoint(""/test"");
  WriteLine(""Triggered!"");
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        Assert.NotNull(program);
        Assert.Single(program.Workflows);

        var workflow = program.Workflows[0];
        Assert.Equal(2, workflow.Body.Count);

        var listen = workflow.Body[0] as Ast.ListenNode;
        Assert.NotNull(listen);
        Assert.Equal("HttpEndpoint", listen!.Activity.ActivityName);
    }

    [Fact(DisplayName = "Parser can parse workflow without workflow keyword")]
    public void Parse_WithoutWorkflowKeyword_ShouldReturnWorkflowWithCorrectStructure()
    {
        // Arrange
        var source = @"WriteLine(""Hello World""); WriteLine(""Great to meet you!"");";

        // Act
        var program = _parser.Parse(source);

        // Assert
        Assert.NotNull(program);
        Assert.Single(program.Workflows);

        var workflow = program.Workflows[0];
        Assert.Equal(2, workflow.Body.Count);
    }

    [Fact(DisplayName = "Parser can parse complex workflow with variables, listen statements, and ElsaScript expressions")]
    public void Parse_WithComplexWorkflow_ShouldReturnWorkflowWithCorrectStructure()
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
        var program = _parser.Parse(source);

        // Assert
        Assert.NotNull(program);
        Assert.Single(program.Workflows);

        var workflow = program.Workflows[0];
        Assert.Equal("HelloWorldHttpDsl", workflow.Id);

        // Verify global use statement
        Assert.Single(program.GlobalUseStatements);
        var useStatement = program.GlobalUseStatements[0];
        Assert.Equal(Ast.UseType.Expressions, useStatement.Type);
        Assert.Equal("js", useStatement.Value);

        // No workflow-level use statements
        Assert.Empty(workflow.UseStatements);

        // Verify body contains: var declaration, listen statement, WriteLine, WriteHttpResponse
        Assert.Equal(4, workflow.Body.Count);

        // Verify variable declaration
        var varDecl = workflow.Body[0] as Ast.VariableDeclarationNode;
        Assert.NotNull(varDecl);
        Assert.Equal("message", varDecl!.Name);
        Assert.Equal(Ast.VariableKind.Var, varDecl.Kind);

        // Verify listen statement
        var listenNode = workflow.Body[1] as Ast.ListenNode;
        Assert.NotNull(listenNode);
        Assert.Equal("HttpEndpoint", listenNode!.Activity.ActivityName);
        Assert.Single(listenNode.Activity.Arguments);

        // Verify WriteLine with ElsaScript expression
        var writeLineNode = workflow.Body[2] as Ast.ActivityInvocationNode;
        Assert.NotNull(writeLineNode);
        Assert.Equal("WriteLine", writeLineNode!.ActivityName);
        Assert.Single(writeLineNode.Arguments);
        var writeLineExpr = writeLineNode.Arguments[0].Value as Ast.ElsaExpressionNode;
        Assert.NotNull(writeLineExpr);
        Assert.Equal("js", writeLineExpr!.Language);

        // Verify WriteHttpResponse with variable reference
        var writeHttpNode = workflow.Body[3] as Ast.ActivityInvocationNode;
        Assert.NotNull(writeHttpNode);
        Assert.Equal("WriteHttpResponse", writeHttpNode!.ActivityName);
        Assert.Single(writeHttpNode.Arguments);
        var writeHttpArg = writeHttpNode.Arguments[0].Value as Ast.IdentifierNode;
        Assert.NotNull(writeHttpArg);
        Assert.Equal("message", writeHttpArg!.Name);
    }

    [Fact(DisplayName = "Parser can parse for loop with 'to' keyword (exclusive)")]
    public void Parse_WithForLoopExclusive_ShouldReturnWorkflowWithForNode()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ""ForLoopTest"" {
  for (var i = 0 to 10 step 1)
  {
    WriteLine(js => `Step: ${i}`)
  }
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        Assert.NotNull(program);
        Assert.Single(program.Workflows);

        var workflow = program.Workflows[0];
        Assert.Equal("ForLoopTest", workflow.Id);
        Assert.Single(workflow.Body);

        var forNode = workflow.Body[0] as Ast.ForNode;
        Assert.NotNull(forNode);
        Assert.True(forNode!.DeclaresVariable); // var i
        Assert.Equal("i", forNode.VariableName);
        Assert.False(forNode.IsInclusive);

        var startLiteral = forNode.Start as Ast.LiteralNode;
        Assert.NotNull(startLiteral);
        // Numbers are parsed as decimals by the parser
        Assert.Equal(0m, Convert.ToDecimal(startLiteral!.Value!));

        var endLiteral = forNode.End as Ast.LiteralNode;
        Assert.NotNull(endLiteral);
        Assert.Equal(10m, Convert.ToDecimal(endLiteral!.Value!));

        var stepLiteral = forNode.Step as Ast.LiteralNode;
        Assert.NotNull(stepLiteral);
        Assert.Equal(1m, Convert.ToDecimal(stepLiteral!.Value!));
    }

    [Fact(DisplayName = "Parser can parse for loop with 'through' keyword (inclusive)")]
    public void Parse_WithForLoopInclusive_ShouldReturnWorkflowWithForNode()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ""ForLoopInclusiveTest"" {
  for (var i = 0 through 10 step 1)
  {
    WriteLine(js => `Step: ${i}`)
  }
}";

        // Act
        var program = _parser.Parse(source);

        // Assert
        Assert.NotNull(program);
        Assert.Single(program.Workflows);

        var workflow = program.Workflows[0];
        Assert.Equal("ForLoopInclusiveTest", workflow.Id);
        Assert.Single(workflow.Body);

        var forNode = workflow.Body[0] as Ast.ForNode;
        Assert.NotNull(forNode);
        Assert.True(forNode!.DeclaresVariable); // var i
        Assert.Equal("i", forNode.VariableName);
        Assert.True(forNode.IsInclusive);
    }

    [Fact(DisplayName = "Parser can parse workflow with metadata")]
    public void Parse_WithWorkflowMetadata_ShouldReturnWorkflowWithCorrectMetadata()
    {
        // Arrange
        var source = @"
use Elsa.Activities.Console;

workflow ""HelloWorldDsl""(
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
        Assert.NotNull(program);
        Assert.Single(program.Workflows);

        var workflow = program.Workflows[0];
        Assert.Equal("HelloWorldDsl", workflow.Id);

        // Check metadata
        Assert.Equal("Hello World DSL", (string)workflow.Metadata["DisplayName"]);
        Assert.Equal("Demonstrates ElsaScript with metadata", (string)workflow.Metadata["Description"]);
        Assert.Equal("hello-world-dsl", (string)workflow.Metadata["DefinitionId"]);
        Assert.Equal("hello-world-dsl-v1", (string)workflow.Metadata["DefinitionVersionId"]);
        Assert.Equal(1L, Convert.ToInt64(workflow.Metadata["Version"]));
        Assert.True(Convert.ToBoolean(workflow.Metadata["UsableAsActivity"]));

        // Check use statements (workflow-level only, global handled separately)
        Assert.Single(workflow.UseStatements);
        Assert.Equal(Ast.UseType.Expressions, workflow.UseStatements[0].Type);
        Assert.Equal("js", workflow.UseStatements[0].Value);

        // Check body
        Assert.Single(workflow.Body);
    }

}
