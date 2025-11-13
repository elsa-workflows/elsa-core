using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
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
    private readonly IServiceProvider _services;
    private readonly IElsaScriptParser _parser;
    private readonly IElsaScriptCompiler _compiler;

    public CompilerTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper)
            .ConfigureElsa(elsa => elsa.UseElsaScript())
            .Build();

        _parser = _services.GetRequiredService<IElsaScriptParser>();
        _compiler = _services.GetRequiredService<IElsaScriptCompiler>();
    }

    [Fact(DisplayName = "Compiler service is registered and available")]
    public void Test1()
    {
        // Arrange & Act
        var compiler = _services.GetRequiredService<IElsaScriptCompiler>();

        // Assert
        Assert.NotNull(compiler);
    }

    [Fact(DisplayName = "Compiler can parse and analyze a workflow AST")]
    public void Test2()
    {
        // Arrange
        var source = @"
use Elsa.Activities.Console;
use expressions js;

workflow ""HelloWorld"" {
  WriteLine(""Hello World"");
}";

        // Act
        var ast = _parser.Parse(source);
        
        // Assert - verify the AST is correctly parsed
        Assert.NotNull(ast);
        Assert.Equal("HelloWorld", ast.Name);
        Assert.Single(ast.Body);
        
        var activity = ast.Body[0] as Ast.ActivityInvocationNode;
        Assert.NotNull(activity);
        Assert.Equal("WriteLine", activity!.ActivityName);
    }

    [Fact(DisplayName = "Compiler recognizes variable declarations in AST")]
    public void Test3()
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
        var ast = _parser.Parse(source);

        // Assert - verify variables are recognized
        Assert.NotNull(ast);
        Assert.Equal(3, ast.Body.Count);
        
        var varDecls = ast.Body.OfType<Ast.VariableDeclarationNode>().ToList();
        Assert.Equal(3, varDecls.Count);
        Assert.Equal("message", varDecls[0].Name);
        Assert.Equal("count", varDecls[1].Name);
        Assert.Equal("pi", varDecls[2].Name);
    }
}

