using Elsa.Expressions.Contracts;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Tests for JintJavaScriptEvaluator to ensure all custom functions remain available.
/// These tests protect against accidental renaming or removal of JavaScript functions.
/// </summary>
public class JintJavaScriptEvaluatorTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Theory(DisplayName = "Common workflow functions should be available")]
    [InlineData("getWorkflowDefinitionId")]
    [InlineData("getWorkflowDefinitionVersionId")]
    [InlineData("getWorkflowDefinitionVersion")]
    [InlineData("getWorkflowInstanceId")]
    [InlineData("getCorrelationId")]
    [InlineData("getWorkflowInstanceName")]
    public async Task Common_Workflow_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        Assert.Equal("function", result);
    }

    [Theory(DisplayName = "Workflow mutator functions should be available")]
    [InlineData("setCorrelationId")]
    [InlineData("setWorkflowInstanceName")]
    [InlineData("setVariable")]
    public async Task Workflow_Mutator_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        Assert.Equal("function", result);
    }

    [Theory(DisplayName = "Variable and input/output accessor functions should be available")]
    [InlineData("getVariable")]
    [InlineData("getInput")]
    [InlineData("getOutputFrom")]
    [InlineData("getLastResult")]
    public async Task Variable_And_IO_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        Assert.Equal("function", result);
    }

    [Theory(DisplayName = "String utility functions should be available")]
    [InlineData("isNullOrWhiteSpace")]
    [InlineData("isNullOrEmpty")]
    public async Task String_Utility_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        Assert.Equal("function", result);
    }

    [Theory(DisplayName = "GUID functions should be available")]
    [InlineData("parseGuid")]
    [InlineData("newGuid")]
    [InlineData("newGuidString")]
    [InlineData("newShortGuid")]
    [InlineData("getGuidString")] // Deprecated but should still exist
    [InlineData("getShortGuid")] // Deprecated but should still exist
    public async Task GUID_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        Assert.Equal("function", result);
    }

    [Theory(DisplayName = "Encoding and serialization functions should be available")]
    [InlineData("toJson")]
    [InlineData("bytesToString")]
    [InlineData("bytesFromString")]
    [InlineData("bytesToBase64")]
    [InlineData("bytesFromBase64")]
    [InlineData("stringToBase64")]
    [InlineData("stringFromBase64")]
    [InlineData("streamToBytes")]
    [InlineData("streamToBase64")]
    public async Task Encoding_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        Assert.Equal("function", result);
    }

    [Fact(DisplayName = "Variable accessors should be created for workflow variables")]
    public async Task Variable_Accessors_Should_Be_Created()
    {
        // Arrange
        var script = "return typeof getMyVariable;";
        var context = await CreateExpressionExecutionContextAsync(variables:
        [
            new Variable<int>("MyVariable", 42)
        ]);
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - getter function should be created for the variable
        Assert.Equal("function", result);
    }

    [Fact(DisplayName = "Variable setter accessors should be created for workflow variables")]
    public async Task Variable_Setter_Accessors_Should_Be_Created()
    {
        // Arrange
        var script = "return typeof setMyVariable;";
        var context = await CreateExpressionExecutionContextAsync(variables:
        [
            new Variable<int>("MyVariable", 42)
        ]);
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - setter function should be created for the variable
        Assert.Equal("function", result);
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
