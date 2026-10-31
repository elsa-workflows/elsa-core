using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using static Elsa.Workflows.IntegrationTests.Evaluation.EvaluationTestHelpers;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputStateStorageTests
{
    [Theory(DisplayName = "Stores primitive values in activity state")]
    [InlineData(42)]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task StoresPrimitiveIntValues(int expectedValue)
    {
        // Arrange
        var variable = new Variable<int>("intVar", 0, "intVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(expectedValue));
        var context = await CreateContextAsync(setVariable);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Contains(expectedValue, context.ActivityState.Values);
    }

    [Theory(DisplayName = "Stores boolean values in activity state")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StoresBooleanValues(bool expectedValue)
    {
        // Arrange
        var variable = new Variable<bool>("boolVar", false, "boolVar");
        var setVariable = new SetVariable<bool>(variable, new Input<bool>(expectedValue));
        var context = await CreateContextAsync(setVariable);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Contains(expectedValue, context.ActivityState.Values);
    }

    [Theory(DisplayName = "Stores string values in activity state")]
    [InlineData("Test String")]
    [InlineData("")]
    public async Task StoresStringValues(string expectedValue)
    {
        // Arrange
        var variable = new Variable<string>("stringVar", "", "stringVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>(expectedValue));
        var context = await CreateContextAsync(setVariable);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Contains(expectedValue, context.ActivityState.Values);
    }

    [Fact(DisplayName = "Stores values using input descriptor name as key")]
    public async Task StoresValuesByInputDescriptorName()
    {
        // Arrange
        const string textValue = "Named Input Test";
        var writeLine = new WriteLine(textValue);
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.ActivityState.ContainsKey("Text"));
        Assert.Equal(textValue, context.ActivityState["Text"]);
    }

    [Fact(DisplayName = "Stores null values correctly")]
    public async Task StoresNullValues()
    {
        // Arrange
        var writeLine = new WriteLine(new Input<string>(default(string)!));
        var context = await CreateContextAsync(writeLine);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.ActivityState.ContainsKey("Text"));
        Assert.Null(context.ActivityState["Text"]);
    }

    [Fact(DisplayName = "Overwrites previous values on re-evaluation")]
    public async Task OverwritesPreviousValuesOnReEvaluation()
    {
        // Arrange
        const string initialValue = "Initial";
        const string updatedValue = "Updated";
        var writeLine = new WriteLine(initialValue);
        var context = await CreateContextAsync(writeLine);

        // Act - First evaluation
        await context.EvaluateInputPropertiesAsync();
        var firstValue = context.ActivityState["Text"];

        // Modify and re-evaluate
        writeLine.Text = new(updatedValue);
        await context.EvaluateInputPropertyAsync("Text");
        var secondValue = context.ActivityState["Text"];

        // Assert
        Assert.Equal(initialValue, firstValue);
        Assert.Equal(updatedValue, secondValue);
    }

    [Fact(DisplayName = "Stores multiple inputs separately")]
    public async Task StoresMultipleInputsSeparately()
    {
        // Arrange
        var variable = new Variable<int>("multiVar", 100, "multiVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(200));
        var context = await CreateContextAsync(setVariable);

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.ActivityState.Count >= 1);
    }

    [Fact(DisplayName = "Maintains state across multiple evaluations")]
    public async Task MaintainsStateAcrossMultipleEvaluations()
    {
        // Arrange
        const string value1 = "First";
        var writeLine1 = new WriteLine(value1);
        var context = await CreateContextAsync(writeLine1);

        // Act - First evaluation
        await context.EvaluateInputPropertiesAsync();
        var stateCountAfterFirst = context.ActivityState.Count;

        // Second evaluation of same input
        await context.EvaluateInputPropertyAsync("Text");
        var stateCountAfterSecond = context.ActivityState.Count;

        // Assert
        Assert.Equal(stateCountAfterFirst, stateCountAfterSecond);
        Assert.Equal(value1, context.ActivityState["Text"]);
    }
}
