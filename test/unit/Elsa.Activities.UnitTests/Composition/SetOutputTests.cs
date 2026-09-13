using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Management.Activities.SetOutput;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Composition;

public class SetOutputTests
{
    private const string DefaultOutputName = "Result";

    [Test]
    [Arguments("test output")]
    [Arguments("string value")]
    [Arguments(42)]
    [Arguments(true)]
    [Arguments(3.14)]
    [Arguments("")]
    public async Task Should_Set_Workflow_Output_With_Value(object expectedValue)
    {
        // Act
        var context = await ExecuteSetOutputAsync(DefaultOutputName, expectedValue);

        // Assert
        await AssertOutputEquals(context, DefaultOutputName, expectedValue);
    }

    [Test]
    public async Task Should_Set_Workflow_Output_With_Null_Value()
    {
        // Act
        var context = await ExecuteSetOutputAsync(DefaultOutputName, null);

        // Assert
        await Assert.That(context.WorkflowExecutionContext.Output.ContainsKey(DefaultOutputName)).IsTrue();
        await Assert.That(context.WorkflowExecutionContext.Output[DefaultOutputName]).IsNull();
    }

    [Test]
    public async Task Should_Set_Output_With_Complex_Object()
    {
        // Arrange
        var expectedValue = new { Name = "Test", Value = 42 };

        // Act
        var context = await ExecuteSetOutputAsync(DefaultOutputName, expectedValue);

        // Assert
        await AssertOutputEquals(context, DefaultOutputName, expectedValue);
    }

    [Test]
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
        await AssertOutputEquals(context1, outputName, firstValue);
        await AssertOutputEquals(context2, outputName, secondValue);
    }

    [Test]
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
        await AssertOutputEquals(context1, output1Name, value1);
        await AssertOutputEquals(context2, output1Name, value1);
        await AssertOutputEquals(context2, output2Name, value2);
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

    private static async Task AssertOutputEquals(ActivityExecutionContext context, string outputName, object? expectedValue)
    {
        var actualValue = context.WorkflowExecutionContext.Output[outputName];
        await Assert.That(actualValue).IsEqualTo(expectedValue);
    }
}
