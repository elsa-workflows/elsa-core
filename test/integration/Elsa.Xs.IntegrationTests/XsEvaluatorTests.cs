using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Expressions.Xs.Contracts;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Xs.IntegrationTests;

/// <summary>
/// Tests for XsEvaluator to ensure the XS expression provider works correctly.
/// </summary>
public class XsEvaluatorTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(testOutputHelper)
        .ConfigureElsa(elsa => elsa.UseXs());

    [Fact(DisplayName = "Simple arithmetic expression should evaluate correctly")]
    public async Task Simple_Arithmetic_Expression_Should_Evaluate()
    {
        // Arrange
        var script = "1 + 2;";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IXsEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(int), context, new ExpressionEvaluatorOptions());

        // Assert
        Assert.Equal(3, result);
    }

    [Fact(DisplayName = "Variable declaration and usage should work")]
    public async Task Variable_Declaration_And_Usage_Should_Work()
    {
        // Arrange
        var script = """
        var x = 10;
        var y = 20;
        x + y;
        """;
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IXsEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(int), context, new ExpressionEvaluatorOptions());

        // Assert
        Assert.Equal(30, result);
    }

    [Fact(DisplayName = "String concatenation should work")]
    public async Task String_Concatenation_Should_Work()
    {
        // Arrange
        var script = """
        var hello = "Hello";
        var world = "World";
        hello + " " + world;
        """;
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IXsEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context, new ExpressionEvaluatorOptions());

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact(DisplayName = "If-else expressions should work")]
    public async Task IfElse_Expression_Should_Work()
    {
        // Arrange
        var script = """
        var x = 10;
        if (x > 5)
        {
            "Greater";
        }
        else
        {
            "Less";
        }
        """;
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IXsEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context, new ExpressionEvaluatorOptions());

        // Assert
        Assert.Equal("Greater", result);
    }

    [Fact(DisplayName = "Array operations should work")]
    public async Task Array_Operations_Should_Work()
    {
        // Arrange
        var script = """
        var numbers = new int[] { 1, 2, 3, 4, 5 };
        numbers[2];
        """;
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IXsEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(int), context, new ExpressionEvaluatorOptions());

        // Assert
        Assert.Equal(3, result);
    }

    [Fact(DisplayName = "Expression handler should evaluate XS expressions")]
    public async Task Expression_Handler_Should_Evaluate()
    {
        // Arrange
        await _fixture.BuildAsync();
        var expressionEvaluator = _fixture.Services.GetRequiredService<IExpressionEvaluator>();
        var context = await CreateExpressionExecutionContextAsync();
        
        var expression = new Expression("XS", "10 + 20;");

        // Act
        var result = await expressionEvaluator.EvaluateAsync(expression, typeof(int), context);

        // Assert
        Assert.Equal(30, result);
    }

    [Fact(DisplayName = "Multiple statements should return the last expression")]
    public async Task Multiple_Statements_Should_Return_Last_Expression()
    {
        // Arrange
        var script = """
        var a = 1;
        var b = 2;
        var c = 3;
        a + b + c;
        """;
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IXsEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(int), context, new ExpressionEvaluatorOptions());

        // Assert
        Assert.Equal(6, result);
    }

    [Fact(DisplayName = "Boolean expressions should work")]
    public async Task Boolean_Expressions_Should_Work()
    {
        // Arrange
        var script = """
        var x = 10;
        var y = 20;
        x < y;
        """;
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IXsEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(bool), context, new ExpressionEvaluatorOptions());

        // Assert
        Assert.True((bool)result!);
    }

    private async Task<ExpressionExecutionContext> CreateExpressionExecutionContextAsync(Variable[]? variables = null)
    {
        await _fixture.BuildAsync();

        var workflow = new Elsa.Workflows.Activities.Workflow();

        if (variables != null)
        {
            foreach (var variable in variables)
            {
                workflow.Variables.Add(variable);
            }
        }

        var result = await _fixture.RunActivityAsync(workflow);
        var activityContext = result.Journal.ActivityExecutionContexts.First();

        return new ExpressionExecutionContext(
            _fixture.Services,
            activityContext.ExpressionExecutionContext.Memory,
            cancellationToken: default);
    }
}
