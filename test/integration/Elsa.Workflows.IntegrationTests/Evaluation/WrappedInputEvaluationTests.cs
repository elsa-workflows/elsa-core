using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using static Elsa.Workflows.IntegrationTests.Evaluation.EvaluationTestHelpers;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class WrappedInputEvaluationTests
{
    [Fact(DisplayName = "Sets memory block reference with deterministic ID")]
    public async Task SetsMemoryBlockReferenceWithDeterministicId()
    {
        // Arrange
        const string expectedValue = "Test Value";
        var writeLine = new WriteLine(new Input<string>(expectedValue));
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        var memoryReference = writeLine.Text.MemoryBlockReference();
        Assert.NotNull(memoryReference);
        var storedValue = memoryReference.Get(context.ExpressionExecutionContext);
        Assert.Equal(expectedValue, storedValue);
    }

    [Fact(DisplayName = "Handles missing memory block reference ID")]
    public async Task HandlesMissingMemoryBlockReferenceId()
    {
        // Arrange
        const string expectedValue = "Memory Test";
        var writeLine = new WriteLine(new Input<string>(expectedValue));
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        var memoryReference = writeLine.Text.MemoryBlockReference();
        Assert.NotNull(memoryReference);
        Assert.NotNull(memoryReference.Id);
        Assert.False(string.IsNullOrEmpty(memoryReference.Id));
    }

    [Fact(DisplayName = "Stores evaluated value in ExpressionExecutionContext")]
    public async Task StoresEvaluatedValueInExpressionExecutionContext()
    {
        // Arrange
        const int expectedValue = 123;
        var variable = new Variable<int>("testVar", 0, "testVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(expectedValue));
        var context = await CreateContextAsync(setVariable);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        var memoryReference = setVariable.Value.MemoryBlockReference();
        var storedValue = memoryReference.Get(context.ExpressionExecutionContext);
        Assert.Equal(expectedValue, storedValue);
    }

    [Theory(DisplayName = "Evaluates different expression types correctly")]
    [InlineData("Literal", "Literal Test")]
    [InlineData("Delegate", "Delegate Result")]
    [InlineData("Variable", "Variable Value")]
    public async Task EvaluatesExpressionTypes(string expressionType, string expectedValue)
    {
        // Arrange
        WriteLine writeLine;
        Variable<string>? variable = null;

        switch (expressionType)
        {
            case "Literal":
                var literal = new Literal<string>(expectedValue);
                writeLine = new(new Input<string>(literal));
                break;
            case "Delegate":
                writeLine = new(new Input<string>(() => expectedValue));
                break;
            case "Variable":
                variable = new("myVar", expectedValue, "myVar");
                writeLine = new(new Input<string>(variable));
                break;
            default:
                throw new ArgumentException("Invalid expression type");
        }

        var context = await CreateContextAsync(writeLine);

        variable?.Set(context.ExpressionExecutionContext, expectedValue);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Equal(expectedValue, context.ActivityState["Text"]);
    }
}
