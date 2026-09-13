using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputEvaluationTests : EvaluationTestBase
{
    [Test]
    [DisplayName("Evaluates literal input values correctly: $inputValue")]
    [Arguments("Literal Value")]
    [Arguments("")]
    [Arguments("Special chars: !@#$%")]
    public async Task EvaluatesLiteralInput(string inputValue)
    {
        // Arrange
        var writeLine = new WriteLine(inputValue);
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.ActivityState.ContainsKey("Text")).IsTrue();
        await Assert.That(context.ActivityState["Text"]).IsEqualTo(inputValue);
    }

    [Test]
    [DisplayName("Evaluates delegate input expressions")]
    public async Task EvaluatesDelegateInput()
    {
        // Arrange
        const string original = "Original";
        var writeLine = new WriteLine(new Input<string>(() => $"TRANSFORMED_{original}"));
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.ActivityState["Text"]).IsEqualTo("TRANSFORMED_Original");
    }

    [Test]
    [DisplayName("Handles null input gracefully")]
    public async Task HandlesNullInput()
    {
        // Arrange
        var writeLine = new WriteLine(new Input<string>(default(string)!));
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(context.ActivityState["Text"]).IsNull();
    }

    [Test]
    [DisplayName("Stores evaluated value in activity state")]
    public async Task StoresEvaluatedValueInActivityState()
    {
        // Arrange
        const string expectedValue = "Test Value";
        var writeLine = new WriteLine(expectedValue);
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        await Assert.That(writeLine.Text).IsNotNull();
        await Assert.That(context.ActivityState.ContainsKey("Text")).IsTrue();
        await Assert.That(context.ActivityState["Text"]).IsEqualTo(expectedValue);
    }
}
