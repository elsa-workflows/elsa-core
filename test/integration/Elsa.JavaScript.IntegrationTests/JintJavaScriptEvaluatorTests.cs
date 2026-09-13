using Elsa.Expressions.Contracts;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Tests for JintJavaScriptEvaluator to ensure all custom functions remain available.
/// These tests protect against accidental renaming or removal of JavaScript functions.
/// </summary>
public class JintJavaScriptEvaluatorTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("Common workflow functions should be available: $functionName")]
    [Arguments("getWorkflowDefinitionId")]
    [Arguments("getWorkflowDefinitionVersionId")]
    [Arguments("getWorkflowDefinitionVersion")]
    [Arguments("getWorkflowInstanceId")]
    [Arguments("getCorrelationId")]
    [Arguments("getWorkflowInstanceName")]
    public async Task Common_Workflow_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        await Assert.That(result).IsEqualTo("function");
    }

    [Test]
    [DisplayName("Workflow mutator functions should be available: $functionName")]
    [Arguments("setCorrelationId")]
    [Arguments("setWorkflowInstanceName")]
    [Arguments("setVariable")]
    public async Task Workflow_Mutator_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        await Assert.That(result).IsEqualTo("function");
    }

    [Test]
    [DisplayName("Variable and input/output accessor functions should be available: $functionName")]
    [Arguments("getVariable")]
    [Arguments("getInput")]
    [Arguments("getOutputFrom")]
    [Arguments("getLastResult")]
    public async Task Variable_And_IO_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        await Assert.That(result).IsEqualTo("function");
    }

    [Test]
    [DisplayName("String utility functions should be available: $functionName")]
    [Arguments("isNullOrWhiteSpace")]
    [Arguments("isNullOrEmpty")]
    public async Task String_Utility_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        await Assert.That(result).IsEqualTo("function");
    }

    [Test]
    [DisplayName("GUID functions should be available: $functionName")]
    [Arguments("parseGuid")]
    [Arguments("newGuid")]
    [Arguments("newGuidString")]
    [Arguments("newShortGuid")]
    [Arguments("getGuidString")] // Deprecated but should still exist
    [Arguments("getShortGuid")] // Deprecated but should still exist
    public async Task GUID_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        await Assert.That(result).IsEqualTo("function");
    }

    [Test]
    [DisplayName("Encoding and serialization functions should be available: $functionName")]
    [Arguments("toJson")]
    [Arguments("bytesToString")]
    [Arguments("bytesFromString")]
    [Arguments("bytesToBase64")]
    [Arguments("bytesFromBase64")]
    [Arguments("stringToBase64")]
    [Arguments("stringFromBase64")]
    [Arguments("streamToBytes")]
    [Arguments("streamToBase64")]
    public async Task Encoding_Functions_Should_Be_Available(string functionName)
    {
        // Arrange
        var script = $"return typeof {functionName};";
        var context = await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();

        // Act
        var result = await evaluator.EvaluateAsync(script, typeof(string), context) as string;

        // Assert - function should exist (not 'undefined')
        await Assert.That(result).IsEqualTo("function");
    }

    [Test]
    [DisplayName("Variable accessors should be created for workflow variables")]
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
        await Assert.That(result).IsEqualTo("function");
    }

    [Test]
    [DisplayName("Variable setter accessors should be created for workflow variables")]
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
        await Assert.That(result).IsEqualTo("function");
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
