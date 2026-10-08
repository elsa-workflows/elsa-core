using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using static Elsa.Workflows.IntegrationTests.Evaluation.EvaluationTestHelpers;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputEvaluationTests
{
    [Theory(DisplayName = "Evaluates literal input values correctly")]
    [InlineData("Literal Value")]
    [InlineData("")]
    [InlineData("Special chars: !@#$%")]
    public async Task EvaluatesLiteralInput(string inputValue)
    {
        // Arrange
        var writeLine = new WriteLine(inputValue);
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.ActivityState.ContainsKey("Text"));
        Assert.Equal(inputValue, context.ActivityState["Text"]);
    }

    [Fact(DisplayName = "Evaluates delegate input expressions")]
    public async Task EvaluatesDelegateInput()
    {
        // Arrange
        const string original = "Original";
        var writeLine = new WriteLine(new Input<string>(() => $"TRANSFORMED_{original}"));
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Equal("TRANSFORMED_Original", context.ActivityState["Text"]);
    }

    [Fact(DisplayName = "Handles null input gracefully")]
    public async Task HandlesNullInput()
    {
        // Arrange
        var writeLine = new WriteLine(new Input<string>(default(string)!));
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Null(context.ActivityState["Text"]);
    }

    [Fact(DisplayName = "Stores evaluated value in activity state")]
    public async Task StoresEvaluatedValueInActivityState()
    {
        // Arrange
        const string expectedValue = "Test Value";
        var writeLine = new WriteLine(expectedValue);
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.NotNull(writeLine.Text);
        Assert.True(context.ActivityState.ContainsKey("Text"));
        Assert.Equal(expectedValue, context.ActivityState["Text"]);
    }
}
