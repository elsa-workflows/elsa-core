using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputPropertyEvaluationTests : EvaluationTestBase
{
    [Test]
    [DisplayName("Evaluates all auto-evaluate inputs")]
    public async Task EvaluatesAllAutoEvaluateInputs()
    {
        // Arrange
        const string expectedText = "Hello, World!";
        var writeLine = new WriteLine(expectedText);
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.ActivityState.Values).Contains(expectedText);
    }

    [Test]
    [DisplayName("Evaluates specific input by name")]
    public async Task EvaluatesSpecificInputByName()
    {
        // Arrange
        const string expectedValue = "Test Value";
        var writeLine = new WriteLine(expectedValue);
        var context = await CreateContextAsync(writeLine);

        // Act
        var result = await context.EvaluateInputPropertyAsync("Text");

        // Assert
        await Assert.That(result).IsEqualTo(expectedValue);
        await Assert.That(context.ActivityState.Values).Contains(expectedValue);
    }

    [Test]
    [DisplayName("Evaluates specific input by property expression")]
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
        await Assert.That(result).IsEqualTo(expectedValue);
    }

    [Test]
    [DisplayName("Throws when input name not found")]
    public async Task ThrowsWhenInputNameNotFound()
    {
        // Arrange
        var writeLine = new WriteLine("Test");
        var context = await CreateContextAsync(writeLine);

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<Exception>(
            async () => await context.EvaluateInputPropertyAsync("NonExistentInput"));

        await Assert.That(exception!.Message).Contains("No input with name NonExistentInput could be found", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("Sets HasEvaluatedProperties flag after evaluation")]
    public async Task SetsHasEvaluatedPropertiesFlag()
    {
        // Arrange
        var writeLine = new WriteLine("Test");
        var context = await CreateContextAsync(writeLine);

        await Assert.That(context.GetHasEvaluatedProperties()).IsFalse();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.GetHasEvaluatedProperties()).IsTrue();
    }
}
