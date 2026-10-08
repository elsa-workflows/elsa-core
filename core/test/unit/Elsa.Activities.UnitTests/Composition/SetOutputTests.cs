using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Management.Activities.SetOutput;

namespace Elsa.Activities.UnitTests.Composition;

public class SetOutputTests
{
    private const string DefaultOutputName = "Result";

    [Theory]
    [InlineData("test output")]
    [InlineData("string value")]
    [InlineData(42)]
    [InlineData(true)]
    [InlineData(3.14)]
    [InlineData("")]
    public async Task Should_Set_Workflow_Output_With_Value(object expectedValue)
    {
        // Act
        var context = await ExecuteSetOutputAsync(DefaultOutputName, expectedValue);

        // Assert
        AssertOutputEquals(context, DefaultOutputName, expectedValue);
    }

    [Fact]
    public async Task Should_Set_Workflow_Output_With_Null_Value()
    {
        // Act
        var context = await ExecuteSetOutputAsync(DefaultOutputName, null);

        // Assert
        Assert.True(context.WorkflowExecutionContext.Output.ContainsKey(DefaultOutputName));
        Assert.Null(context.WorkflowExecutionContext.Output[DefaultOutputName]);
    }

    [Fact]
    public async Task Should_Set_Output_With_Complex_Object()
    {
        // Arrange
        var expectedValue = new { Name = "Test", Value = 42 };

        // Act
        var context = await ExecuteSetOutputAsync(DefaultOutputName, expectedValue);

        // Assert
        AssertOutputEquals(context, DefaultOutputName, expectedValue);
    }

    [Fact]
    public async Task Should_Update_Workflow_Output_Multiple_Times()
    {
        // Arrange
        const string outputName = "Counter";
        var firstValue = 1;
        var secondValue = 2;

        // Act
        var context1 = await ExecuteSetOutputAsync(outputName, firstValue);
        var context2 = await ExecuteSetOutputAsync(outputName, secondValue,
            ctx => ctx.WorkflowExecutionContext.Output[outputName] = firstValue);

        // Assert
        AssertOutputEquals(context1, outputName, firstValue);
        AssertOutputEquals(context2, outputName, secondValue);
    }

    [Fact]
    public async Task Should_Set_Different_Output_Names()
    {
        // Arrange
        const string output1Name = "FirstOutput";
        const string output2Name = "SecondOutput";
        const string value1 = "value1";
        const int value2 = 42;

        // Act
        var context1 = await ExecuteSetOutputAsync(output1Name, value1);
        var context2 = await ExecuteSetOutputAsync(output2Name, value2,
            ctx => ctx.WorkflowExecutionContext.Output[output1Name] = value1);

        // Assert
        AssertOutputEquals(context1, output1Name, value1);
        AssertOutputEquals(context2, output1Name, value1);
        AssertOutputEquals(context2, output2Name, value2);
    }

    private static async Task<ActivityExecutionContext> ExecuteSetOutputAsync(
        string outputName,
        object? outputValue,
        Action<ActivityExecutionContext>? configureContext = null)
    {
        var setOutput = CreateSetOutputActivity(outputName, outputValue);
        var fixture = new ActivityTestFixture(setOutput);

        if (configureContext != null)
            fixture.ConfigureContext(configureContext);

        return await fixture.ExecuteAsync();
    }

    private static SetOutput CreateSetOutputActivity(string outputName, object? outputValue) => new()
    {
        OutputName = new(outputName),
        OutputValue = new(outputValue)
    };

    private static void AssertOutputEquals(ActivityExecutionContext context, string outputName, object? expectedValue)
    {
        var actualValue = context.WorkflowExecutionContext.Output[outputName];
        Assert.Equal(expectedValue, actualValue);
    }
}
