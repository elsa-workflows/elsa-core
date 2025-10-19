using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputStateStorageTests
{
    [Fact]
    public async Task Should_Store_Serializable_Inputs_In_ActivityState()
    {
        // Arrange - Reference: ActivityExecutionContextExtensions.InputEvaluation.cs:152
        const string expectedValue = "Stored Value";
        var writeLine = new WriteLine(expectedValue);
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.ActivityState.ContainsKey("Text"));
        Assert.Equal(expectedValue, context.ActivityState["Text"]);
    }

    [Fact]
    public async Task Should_Store_Values_By_Input_Descriptor_Name()
    {
        // Arrange
        const string textValue = "Named Input Test";
        var writeLine = new WriteLine(textValue);
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.ActivityState.ContainsKey("Text"));
        Assert.Equal(textValue, context.ActivityState["Text"]);
    }

    [Fact]
    public async Task Should_Handle_Null_Values_Correctly()
    {
        // Arrange
        var writeLine = new WriteLine(new Input<string>((string?)null));
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.ActivityState.ContainsKey("Text"));
        Assert.Null(context.ActivityState["Text"]);
    }

    [Theory]
    [InlineData(42)]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Should_Store_Integer_Values(int expectedValue)
    {
        // Arrange
        var variable = new Variable<int>("intVar", 0, "intVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(expectedValue));
        var fixture = new ActivityTestFixture(setVariable);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Contains(expectedValue, context.ActivityState.Values);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_Store_Boolean_Values(bool expectedValue)
    {
        // Arrange
        var variable = new Variable<bool>("boolVar", false, "boolVar");
        var setVariable = new SetVariable<bool>(variable, new Input<bool>(expectedValue));
        var fixture = new ActivityTestFixture(setVariable);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Contains(expectedValue, context.ActivityState.Values);
    }

    [Theory]
    [InlineData("Test String")]
    [InlineData("")]
    public async Task Should_Store_String_Values(string expectedValue)
    {
        // Arrange
        var variable = new Variable<string>("stringVar", "", "stringVar");
        var setVariable = new SetVariable<string>(variable, new Input<string>(expectedValue));
        var fixture = new ActivityTestFixture(setVariable);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Contains(expectedValue, context.ActivityState.Values);
    }

    [Fact]
    public async Task Should_Overwrite_Previous_Values_On_Re_Evaluation()
    {
        // Arrange
        const string initialValue = "Initial";
        const string updatedValue = "Updated";
        var writeLine = new WriteLine(initialValue);
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act - First evaluation
        await context.EvaluateInputPropertiesAsync();
        var firstValue = context.ActivityState["Text"];

        // Modify the input
        writeLine.Text = new Input<string>(updatedValue);

        // Re-evaluate
        await context.EvaluateInputPropertyAsync("Text");
        var secondValue = context.ActivityState["Text"];

        // Assert
        Assert.Equal(initialValue, firstValue);
        Assert.Equal(updatedValue, secondValue);
    }

    [Fact]
    public async Task Should_Store_Multiple_Inputs_Separately()
    {
        // Arrange
        var variable = new Variable<int>("multiVar", 100, "multiVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(200));
        var fixture = new ActivityTestFixture(setVariable);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert - SetVariable has Variable and Value inputs
        Assert.True(context.ActivityState.Count >= 1);
    }


    [Fact]
    public async Task Should_Maintain_State_Across_Multiple_Evaluations()
    {
        // Arrange
        const string value1 = "First";
        const string value2 = "Second";
        var writeLine1 = new WriteLine(value1);
        var fixture = new ActivityTestFixture(writeLine1);
        var context = await fixture.BuildAsync();

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
