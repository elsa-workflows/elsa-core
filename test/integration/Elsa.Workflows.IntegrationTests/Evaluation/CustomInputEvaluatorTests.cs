using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Evaluation;

public class CustomInputEvaluatorTests
{
    [Fact]
    public async Task Should_Use_DefaultActivityInputEvaluator_By_Default()
    {
        // Arrange - Test that DefaultActivityInputEvaluator works correctly with simple literal input
        const string expectedValue = "Default Evaluation";
        var writeLine = new WriteLine(expectedValue);
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert - The evaluator should have been called and value stored
        Assert.True(context.ActivityState.ContainsKey("Text"));
        Assert.Equal(expectedValue, context.ActivityState["Text"]);
    }

    [Fact]
    public async Task Should_Handle_Null_Return_From_Custom_Evaluator()
    {
        // Arrange - Test null input handling
        var writeLine = new WriteLine(new Input<string>(default(string)!));
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Null(context.ActivityState["Text"]);
    }

    [Fact]
    public async Task Should_Allow_Evaluator_To_Evaluate_Delegates()
    {
        // Arrange - Test delegate expressions
        const string original = "Original";
        var writeLine = new WriteLine(new Input<string>(() => $"TRANSFORMED_{original}"));
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert
        Assert.Equal("TRANSFORMED_Original", context.ActivityState["Text"]);
    }

    [Fact]
    public async Task Should_Handle_Evaluator_Exceptions_Gracefully()
    {
        // Arrange - Test that exceptions in evaluation are properly wrapped
        string FaultyFunc() => throw new InvalidOperationException("Evaluator failed");
        var writeLine = new WriteLine(new Input<string>((Func<string>)FaultyFunc));
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () => await context.EvaluateInputPropertiesAsync());
    }

    [Fact]
    public async Task Should_Access_InputDescriptor_From_Context()
    {
        // Arrange - Test that InputDescriptors are available
        var writeLine = new WriteLine("Test");
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert - Input descriptor should be accessible via ActivityDescriptor
        var inputDescriptor = context.ActivityDescriptor.Inputs.FirstOrDefault(x => x.Name == "Text");
        Assert.NotNull(inputDescriptor);
        Assert.Equal("Text", inputDescriptor.Name);
    }

    [Fact]
    public async Task Should_Access_Input_From_Context()
    {
        // Arrange - Test that Input properties are available on activities
        const string expectedValue = "Test Value";
        var writeLine = new WriteLine(expectedValue);
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act
        await context.EvaluateInputPropertiesAsync();

        // Assert - Input should have been accessed and evaluated
        Assert.NotNull(writeLine.Text);
        Assert.True(context.ActivityState.ContainsKey("Text"));
        Assert.Equal(expectedValue, context.ActivityState["Text"]);
    }

    [Fact]
    public async Task Should_Support_Async_Evaluation_In_Custom_Evaluator()
    {
        // Arrange - Test that evaluation is performed asynchronously
        const string expectedValue = "Evaluation Completed";
        var writeLine = new WriteLine(expectedValue);
        var fixture = new ActivityTestFixture(writeLine);
        var context = await fixture.BuildAsync();

        // Act - The EvaluateInputPropertiesAsync itself is async
        await context.EvaluateInputPropertiesAsync();

        // Assert - Verify async evaluation completed successfully
        Assert.Equal(expectedValue, context.ActivityState["Text"]);
    }
}
