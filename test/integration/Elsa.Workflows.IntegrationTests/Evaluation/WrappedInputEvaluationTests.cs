using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class WrappedInputEvaluationTests : EvaluationTestBase
{
    [Test]
    [DisplayName("Sets memory block reference with deterministic ID")]
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
        await Assert.That(memoryReference).IsNotNull();
        var storedValue = memoryReference.Get(context.ExpressionExecutionContext);
        await Assert.That(storedValue).IsEqualTo(expectedValue);
    }

    [Test]
    [DisplayName("Handles missing memory block reference ID")]
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
        await Assert.That(memoryReference).IsNotNull();
        await Assert.That(memoryReference.Id).IsNotNull();
        await Assert.That(string.IsNullOrEmpty(memoryReference.Id)).IsFalse();
    }

    [Test]
    [DisplayName("Stores evaluated value in ExpressionExecutionContext")]
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
        await Assert.That(storedValue).IsEqualTo(expectedValue);
    }

    [Test]
    [DisplayName("Evaluates different expression types correctly: $expressionType")]
    [Arguments("Literal", "Literal Test")]
    [Arguments("Delegate", "Delegate Result")]
    [Arguments("Variable", "Variable Value")]
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
        await Assert.That(context.ActivityState["Text"]).IsEqualTo(expectedValue);
    }
}
