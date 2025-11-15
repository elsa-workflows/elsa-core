using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Extensions;
using Elsa.Testing.Shared;
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
            .ConfigureElsa(elsa => elsa.UseElsaScript())
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
}

