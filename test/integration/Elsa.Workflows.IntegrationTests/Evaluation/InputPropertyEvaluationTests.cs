using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class InputPropertyEvaluationTests
{
    [Fact]
    public async Task Should_Evaluate_All_AutoEvaluate_Inputs_When_Calling_EvaluateInputPropertiesAsync()
    {
        // Arrange
        const string expectedText = "Hello, World!";
        var writeLine = new WriteLine(expectedText);
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.GetHasEvaluatedProperties());
        Assert.Contains(expectedText, context.ActivityState.Values);
    }


    [Fact]
    public async Task Should_Evaluate_Specific_Input_By_Name()
    {
        // Arrange
        const string expectedValue = "Test Value";
        var writeLine = new WriteLine(expectedValue);
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        var result = await context.EvaluateInputPropertyAsync("Text");

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.Contains(expectedValue, context.ActivityState.Values);
    }

    [Fact]
    public async Task Should_Evaluate_Specific_Input_By_Expression()
    {
        // Arrange
        const int expectedValue = 42;
        var variable = new Variable<int>("myVar", expectedValue, "myVar");
        var setVariable = new SetVariable<int>(variable, new Input<int>(expectedValue));
        var fixture = new ActivityTestFixture(setVariable);
        var context = await fixture.BuildAsync();

        // Act
        var result = await context.EvaluateInputPropertyAsync<SetVariable<int>, int>(x => x.Value);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task Should_Throw_When_Input_Name_Not_Found()
    {
        // Arrange
        var writeLine = new WriteLine("Test");
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
            await context.EvaluateInputPropertyAsync("NonExistentInput"));

        Assert.Contains("No input with name NonExistentInput could be found", exception.Message);
    }

    [Fact]
    public async Task Should_Set_HasEvaluatedProperties_Flag_After_Evaluation()
    {
        // Arrange
        var writeLine = new WriteLine("Test");
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Verify flag is initially false
        Assert.False(context.GetHasEvaluatedProperties());

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.GetHasEvaluatedProperties());
    }

    [Fact]
    public async Task Should_Store_Evaluated_Values_In_Activity_State()
    {
        // Arrange
        const string textValue = "Hello, Testing!";
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
    public async Task Should_Evaluate_Multiple_Inputs()
    {
        // Arrange
        const string expectedValue = "updated value";
        var variable = new Variable<string>("result", "initial", "result");
        var setValue = new SetVariable<string>(variable, new Input<string>(expectedValue));
        var fixture = new ActivityTestFixture(setValue);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.True(context.GetHasEvaluatedProperties());
        Assert.Contains(expectedValue, context.ActivityState.Values);
    }

}
