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
    public void Test1()
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
        var workflow = _parser.Parse(source);

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("HelloWorld", workflow.Name);
        Assert.Equal(2, workflow.UseStatements.Count);
        Assert.Equal(2, workflow.Body.Count);
    }

    [Fact(DisplayName = "Parser can parse variable declarations")]
    public void Test2()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ""VariableTest"" {
  var greeting = ""Hello"";
  let count = 42;
  const pi = 3.14;
}";

        // Act
        var workflow = _parser.Parse(source);

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal("VariableTest", workflow.Name);
        Assert.Equal(3, workflow.Body.Count);
        
        var varDecl = workflow.Body[0] as Ast.VariableDeclarationNode;
        Assert.NotNull(varDecl);
        Assert.Equal(Ast.VariableKind.Var, varDecl!.Kind);
        Assert.Equal("greeting", varDecl.Name);
    }

    [Fact(DisplayName = "Parser can parse activity invocations with named arguments")]
    public void Test3()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ""ActivityTest"" {
  WriteLine(Text: ""Hello World"");
}";

        // Act
        var workflow = _parser.Parse(source);

        // Assert
        Assert.NotNull(workflow);
        Assert.Single(workflow.Body);
        
        var activity = workflow.Body[0] as Ast.ActivityInvocationNode;
        Assert.NotNull(activity);
        Assert.Equal("WriteLine", activity!.ActivityName);
        Assert.Single(activity.Arguments);
        Assert.Equal("Text", activity.Arguments[0].Name);
    }

    [Fact(DisplayName = "Parser can parse listen statements", Skip = "Parser needs enhancement to handle multiple statements including listen")]
    public void Test4()
    {
        // Arrange
        var source = @"
use expressions js;

workflow ""ListenTest"" {
  listen HttpEndpoint(""/test"");
  WriteLine(""Triggered!"");
}";

        // Act
        var workflow = _parser.Parse(source);

        // Assert
        Assert.NotNull(workflow);
        Assert.Equal(2, workflow.Body.Count);
        
        var listen = workflow.Body[0] as Ast.ListenNode;
        Assert.NotNull(listen);
        Assert.Equal("HttpEndpoint", listen!.Activity.ActivityName);
    }
}
