using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Models;
using Xunit;

namespace Elsa.Workflows.Core.UnitTests.Contexts;

/// <summary>
/// GetOutputs prefers what the execution recorded in the output register, including a recorded <c>null</c>,
/// and only falls back to the output's memory block when nothing was recorded.
/// </summary>
public class GetOutputsTests
{
    [Fact]
    public async Task GetOutputs_WhenNullWasRecordedForAnOutput_ReturnsNull()
    {
        // Arrange: the memory block holds a different value than the recorded null.
        var activity = new ActivityWithOutputs((context, self) =>
        {
            context.ExpressionExecutionContext.Set(self.Result, "memory value");
            Record(context, "Result", null);
        });

        // Act
        var outputs = (await new ActivityTestFixture(activity).ExecuteAsync()).GetOutputs();

        // Assert
        Assert.Null(outputs["Result"]);
    }

    [Fact]
    public async Task GetOutputs_WhenNullWasRecordedForADictionaryEntry_ReturnsNull()
    {
        // Arrange: the memory block holds a different value than the recorded null.
        var activity = new ActivityWithOutputs((context, self) =>
        {
            context.ExpressionExecutionContext.Set(self.Outputs["name"], "memory value");
            Record(context, "Outputs:name", null);
        });

        // Act
        var outputs = (await new ActivityTestFixture(activity).ExecuteAsync()).GetOutputs();

        // Assert
        var entries = Assert.IsType<Dictionary<string, object?>>(outputs["Outputs"]);
        Assert.Null(entries["name"]);
    }

    [Fact]
    public async Task GetOutputs_WhenNothingWasRecorded_ReadsTheMemoryBlock()
    {
        // Arrange
        var activity = new ActivityWithOutputs((context, self) =>
        {
            context.ExpressionExecutionContext.Set(self.Result, "memory value");
            context.ExpressionExecutionContext.Set(self.Outputs["name"], "memory entry");
        });

        // Act
        var outputs = (await new ActivityTestFixture(activity).ExecuteAsync()).GetOutputs();

        // Assert
        Assert.Equal("memory value", outputs["Result"]);
        var entries = Assert.IsType<Dictionary<string, object?>>(outputs["Outputs"]);
        Assert.Equal("memory entry", entries["name"]);
    }

    [Fact]
    public async Task GetOutputs_WhenAnOutputWasNeverSet_ReturnsNull()
    {
        // Arrange
        var activity = new ActivityWithOutputs((_, _) => { });

        // Act
        var outputs = (await new ActivityTestFixture(activity).ExecuteAsync()).GetOutputs();

        // Assert
        Assert.Null(outputs["Result"]);
        var entries = Assert.IsType<Dictionary<string, object?>>(outputs["Outputs"]);
        Assert.Null(entries["name"]);
    }

    [Fact]
    public async Task GetOutputs_WhenAValueWasSet_ReturnsIt()
    {
        // Arrange
        var activity = new ActivityWithOutputs((context, self) =>
        {
            context.Set(self.Result, "value");
            context.Set(self.Outputs, "name", "entry");
        });

        // Act
        var outputs = (await new ActivityTestFixture(activity).ExecuteAsync()).GetOutputs();

        // Assert
        Assert.Equal("value", outputs["Result"]);
        var entries = Assert.IsType<Dictionary<string, object?>>(outputs["Outputs"]);
        Assert.Equal("entry", entries["name"]);
    }

    private static void Record(ActivityExecutionContext context, string outputName, object? value) =>
        context.WorkflowExecutionContext.GetActivityOutputRegister().Record(context, outputName, value);

    private sealed class ActivityWithOutputs(Action<ActivityExecutionContext, ActivityWithOutputs> execute) : CodeActivity
    {
        public Output<object> Result { get; set; } = new();
        public Dictionary<string, Output<object>> Outputs { get; set; } = new() { ["name"] = new() };

        protected override void Execute(ActivityExecutionContext context) => execute(context, this);
    }
}
