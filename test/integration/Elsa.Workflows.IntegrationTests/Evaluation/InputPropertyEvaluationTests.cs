using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using static Elsa.Workflows.IntegrationTests.Evaluation.EvaluationTestHelpers;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputPropertyEvaluationTests
{
    [Fact(DisplayName = "Evaluates all auto-evaluate inputs")]
    public async Task EvaluatesAllAutoEvaluateInputs()
    {
        // Arrange
        const string expectedText = "Hello, World!";
        var writeLine = new WriteLine(expectedText);
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Contains(expectedText, context.ActivityState.Values);
    }

    [Fact(DisplayName = "Evaluates specific input by name")]
    public async Task EvaluatesSpecificInputByName()
    {
        // Arrange
        const string expectedValue = "Test Value";
        var writeLine = new WriteLine(expectedValue);
        var context = await CreateContextAsync(writeLine);

        // Act
        var result = await context.EvaluateInputPropertyAsync("Text");

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Contains(expectedValue, context.ActivityState.Values);
    }

    [Fact(DisplayName = "Evaluates specific input by property expression")]
    public async Task EvaluatesSpecificInputByExpression()
    {
        // Arrange
        const int expectedValue = 42;
        var variable = new Variable<int>("myVar", expectedValue, "myVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(expectedValue));
        var context = await CreateContextAsync(setVariable);

        // Act
        var result = await context.EvaluateInputPropertyAsync<SetVariable<int>, int>(x => x.Value);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact(DisplayName = "Throws when input name not found")]
    public async Task ThrowsWhenInputNameNotFound()
    {
        // Arrange
        var writeLine = new WriteLine("Test");
        var context = await CreateContextAsync(writeLine);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(
            async () => await context.EvaluateInputPropertyAsync("NonExistentInput"));

        Assert.Contains("No input with name NonExistentInput could be found", exception.Message);
    }

    [Fact(DisplayName = "Sets HasEvaluatedProperties flag after evaluation")]
    public async Task SetsHasEvaluatedPropertiesFlag()
    {
        // Arrange
        var writeLine = new WriteLine("Test");
        var context = await CreateContextAsync(writeLine);

        Assert.False(context.GetHasEvaluatedProperties());

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.GetHasEvaluatedProperties());
    }
}
