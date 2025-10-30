using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.JavaScript.IntegrationTests;

/// <summary>
/// Tests that validate the behavior of JavaScript custom functions, not just their existence.
/// </summary>
public class JintJavaScriptFunctionBehaviorTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "All JavaScript functions should execute without errors (smoke test)")]
    public async Task All_Functions_Should_Execute_Without_Errors()
    {
        // Arrange
        var script = @"
            // Execute all functions that don't require arguments to ensure they work
            var results = {
                // Workflow info functions
                workflowDefId: getWorkflowDefinitionId(),
                workflowDefVersionId: getWorkflowDefinitionVersionId(),
                workflowDefVersion: getWorkflowDefinitionVersion(),
                workflowInstanceId: getWorkflowInstanceId(),
                correlationId: getCorrelationId(),
                workflowName: getWorkflowInstanceName(),

                // GUID functions
                newGuidValue: newGuid(),
                newGuidStringValue: newGuidString(),
                newShortGuidValue: newShortGuid(),

                // Deprecated GUID functions
                getGuidStringValue: getGuidString(),
                getShortGuidValue: getShortGuid(),

                // String utility functions
                isNullOrWhiteSpaceEmpty: isNullOrWhiteSpace(''),
                isNullOrWhiteSpaceText: isNullOrWhiteSpace('text'),
                isNullOrEmptyEmpty: isNullOrEmpty(''),
                isNullOrEmptyText: isNullOrEmpty('text'),

                // Encoding functions
                toJsonValue: toJson({ key: 'value' }),
                stringToBase64Value: stringToBase64('test'),
                bytesToStringValue: bytesToString([72, 101, 108, 108, 111]),
                bytesToBase64Value: bytesToBase64([72, 101, 108, 108, 111]),
            };

            return toJson(results);
        ";

        // Act
        var result = await EvaluateScriptAsync<string>(script);

        // Assert - script should execute and return JSON
        Assert.NotNull(result);
        Assert.Contains("workflowDefId", result);
    }

    [Fact(DisplayName = "GUID functions should return valid formats")]
    public async Task Guid_Functions_Should_Return_Valid_Formats()
    {
        // Arrange
        var script = @"
            return {
                guid: newGuid().toString(),
                guidString: newGuidString(),
                shortGuid: newShortGuid(),
                parsedGuid: parseGuid('12345678-1234-1234-1234-123456789abc').toString()
            };
        ";

        // Act
        var result = await EvaluateScriptAsync<object>(script);

        // Assert
        var dict = result as IDictionary<string, object>;
        Assert.NotNull(dict);

        // Validate GUID formats
        var guidString = dict["guidString"].ToString();
        Assert.Matches("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", guidString ?? "");

        var shortGuid = dict["shortGuid"]?.ToString();
        Assert.NotNull(shortGuid);
        Assert.InRange(shortGuid.Length, 20, 22); // Base64 GUID without padding (can be 20-22 chars)
    }

    [Theory(DisplayName = "Encoding functions should round-trip correctly")]
    [InlineData("String encoding", @"
        var original = 'Hello, World!';
        var base64 = stringToBase64(original);
        var decoded = stringFromBase64(base64);
        return decoded === original;
    ")]
    [InlineData("Bytes encoding", @"
        var original = bytesFromString('Hello');
        var base64 = bytesToBase64(original);
        var decoded = bytesFromBase64(base64);
        var text = bytesToString(decoded);
        return text === 'Hello';
    ")]
    public async Task Encoding_Functions_Should_Round_Trip(string scenario, string script)
    {
        // Act
        var result = await EvaluateScriptAsync<bool>(script);

        // Assert - round-trip should preserve the original data
        Assert.True(result, $"{scenario} failed to round-trip correctly");
    }

    [Theory(DisplayName = "Setter/getter functions should update and retrieve values correctly")]
    [InlineData("setVariable/getVariable", "setVariable('MyVar', 200); return getVariable('MyVar');", 200, "MyVar", 100)]
    [InlineData("Dynamic variable accessors", "setMyVariable(999); return getMyVariable();", 999, "MyVariable", 42)]
    public async Task Variable_Setters_Should_Update_Values(string scenario, string script, int expectedValue, string variableName, int initialValue)
    {
        // Arrange - Create context with variable
        var context = await CreateExpressionExecutionContextAsync(variables:
        [
            new Variable<int>(variableName, initialValue)
        ]);

        // Act
        var result = await EvaluateScriptAsync<int>(script, context);

        // Assert
        Assert.True(result == expectedValue, $"{scenario}: Expected {expectedValue} but got {result}");
    }

    [Theory(DisplayName = "Workflow mutator functions should update workflow properties")]
    [InlineData("setCorrelationId", "setCorrelationId('my-correlation-id'); return getCorrelationId();", "my-correlation-id")]
    [InlineData("setWorkflowInstanceName", "setWorkflowInstanceName('My Custom Workflow Name'); return getWorkflowInstanceName();", "My Custom Workflow Name")]
    public async Task Workflow_Mutators_Should_Update_Properties(string functionName, string script, string expectedValue)
    {
        // Act
        var result = await EvaluateScriptAsync<string>(script);

        // Assert
        Assert.True(result == expectedValue, $"{functionName}: Expected '{expectedValue}' but got '{result}'");
    }

    [Fact(DisplayName = "String utility functions should validate correctly")]
    public async Task String_Utility_Functions_Should_Validate_Correctly()
    {
        // Arrange
        var script = @"
            return {
                emptyIsNullOrWhiteSpace: isNullOrWhiteSpace(''),
                whitespaceIsNullOrWhiteSpace: isNullOrWhiteSpace('   '),
                textIsNullOrWhiteSpace: isNullOrWhiteSpace('text'),
                emptyIsNullOrEmpty: isNullOrEmpty(''),
                whitespaceIsNullOrEmpty: isNullOrEmpty('   '),
                textIsNullOrEmpty: isNullOrEmpty('text')
            };
        ";

        // Act
        var result = await EvaluateScriptAsync<object>(script);

        // Assert
        var dict = result as IDictionary<string, object>;
        Assert.NotNull(dict);

        Assert.True((bool)dict["emptyIsNullOrWhiteSpace"]);
        Assert.True((bool)dict["whitespaceIsNullOrWhiteSpace"]);
        Assert.False((bool)dict["textIsNullOrWhiteSpace"]);

        Assert.True((bool)dict["emptyIsNullOrEmpty"]);
        Assert.False((bool)dict["whitespaceIsNullOrEmpty"]); // Whitespace is not considered empty
        Assert.False((bool)dict["textIsNullOrEmpty"]);
    }

    [Fact(DisplayName = "toJson should serialize objects correctly")]
    public async Task ToJson_Should_Serialize_Objects()
    {
        // Arrange
        var script = @"
            var obj = {
                name: 'Test',
                value: 42,
                nested: { inner: true }
            };
            return toJson(obj);
        ";

        // Act
        var result = await EvaluateScriptAsync<string>(script);

        // Assert - should produce valid JSON
        Assert.NotNull(result);
        Assert.Contains("\"name\"", result);
        Assert.Contains("\"Test\"", result);
        Assert.Contains("\"value\"", result);
        Assert.Contains("42", result);
    }

    private Task<ExpressionExecutionContext> CreateExpressionExecutionContextAsync(Variable[]? variables = null)
    {
        return _fixture.CreateExpressionExecutionContextAsync(variables);
    }

    /// <summary>
    /// Helper method to evaluate a JavaScript script and return the result as the specified type.
    /// Reduces boilerplate code in tests.
    /// </summary>
    private async Task<T> EvaluateScriptAsync<T>(string script, ExpressionExecutionContext? context = null)
    {
        context ??= await CreateExpressionExecutionContextAsync();
        var evaluator = _fixture.Services.GetRequiredService<IJavaScriptEvaluator>();
        var result = await evaluator.EvaluateAsync(script, typeof(T), context);

        return result is T typedResult ? typedResult : (T)Convert.ChangeType(result, typeof(T))!;
    }
}
