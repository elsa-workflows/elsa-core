using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Activities.UnitTests.Branching;

/// <summary>
/// Unit tests for FlowJoin activity covering both token flow and counter flow modes.
/// </summary>
public class FlowJoinTests
{
    [Theory]
    [InlineData(true, FlowJoinMode.WaitAny)]
    [InlineData(true, FlowJoinMode.WaitAll)]
    [InlineData(false, FlowJoinMode.WaitAny)]
    [InlineData(false, FlowJoinMode.WaitAll)]
    public async Task Should_Complete_In_All_Flow_Mode_Combinations(bool useTokenFlow, FlowJoinMode joinMode)
    {
        // Arrange & Act
        var context = await ExecuteWithFlowModeAsync(useTokenFlow, joinMode);

        // Assert
        Assert.True(context.IsCompleted);
    }

    [Theory]
    [InlineData(true, FlowJoinMode.WaitAny)]
    [InlineData(true, FlowJoinMode.WaitAll)]
    [InlineData(false, FlowJoinMode.WaitAny)]
    [InlineData(false, FlowJoinMode.WaitAll)]
    public async Task Should_Execute_In_Flowchart_Context_For_All_Combinations(bool useTokenFlow, FlowJoinMode joinMode)
    {
        // Arrange & Act
        var (context, flowJoin) = await ExecuteInFlowchartWithFlowModeAsync(useTokenFlow, joinMode);

        // Assert
        Assert.NotNull(context);
        
        var expectedMessage = useTokenFlow
            ? $"Token flow mode with {joinMode} should schedule the join activity"
            : $"Counter flow mode with {joinMode} should schedule the join activity as start";

        Assert.True(context.HasScheduledActivity(flowJoin), expectedMessage);
    }

    [Fact]
    public async Task Should_Demonstrate_Token_vs_Counter_Flow_Differences()
    {
        // Arrange
        var joinMode = FlowJoinMode.WaitAll; // Use WaitAll to highlight differences
        
        // Act
        var tokenContext = await ExecuteWithFlowModeAsync(true, joinMode);
        var counterContext = await ExecuteWithFlowModeAsync(false, joinMode);
        
        // Assert
        Assert.True(tokenContext.IsCompleted, "Token flow should always complete");
        Assert.True(counterContext.IsCompleted, "Counter flow should complete without parent context");
    }

    /// <summary>
    /// Creates a FlowJoin activity with the specified mode.
    /// </summary>
    private static FlowJoin CreateFlowJoin(FlowJoinMode joinMode) => new() { Mode = new(joinMode) };

    /// <summary>
    /// Creates a simple flowchart with the FlowJoin as the start activity.
    /// </summary>
    private static Flowchart CreateSimpleFlowchart(FlowJoin flowJoin) => new()
    {
        Start = flowJoin,
        Activities = { flowJoin }
    };

    /// <summary>
    /// Executes a FlowJoin activity with the specified flow mode, handling the UseTokenFlow setup and teardown.
    /// </summary>
    private static async Task<ActivityExecutionContext> ExecuteWithFlowModeAsync(bool useTokenFlow, FlowJoinMode joinMode)
    {
        return await WithFlowModeAsync(useTokenFlow, async () =>
        {
            var flowJoin = CreateFlowJoin(joinMode);
            return await ExecuteAsync(flowJoin);
        });
    }

    /// <summary>
    /// Executes a FlowJoin activity within a flowchart context with the specified flow mode.
    /// </summary>
    private static async Task<(ActivityExecutionContext context, FlowJoin flowJoin)> ExecuteInFlowchartWithFlowModeAsync(bool useTokenFlow, FlowJoinMode joinMode)
    {
        return await WithFlowModeAsync(useTokenFlow, async () =>
        {
            var flowJoin = CreateFlowJoin(joinMode);
            var flowchart = CreateSimpleFlowchart(flowJoin);
            var context = await ExecuteFlowchartAsync(flowchart);
            return (context, flowJoin);
        });
    }

    /// <summary>
    /// Executes an action with the specified flow mode, ensuring proper setup and teardown of UseTokenFlow.
    /// </summary>
    private static async Task<T> WithFlowModeAsync<T>(bool useTokenFlow, Func<Task<T>> action)
    {
        var originalValue = Flowchart.UseTokenFlow;
        Flowchart.UseTokenFlow = useTokenFlow;

        try
        {
            return await action();
        }
        finally
        {
            Flowchart.UseTokenFlow = originalValue;
        }
    }

    /// <summary>
    /// Executes an activity using the ActivityTestFixture.
    /// </summary>
    private static async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return await new ActivityTestFixture(activity).ExecuteAsync();
    }

    /// <summary>
    /// Executes a flowchart using the ActivityTestFixture.
    /// </summary>
    private static async Task<ActivityExecutionContext> ExecuteFlowchartAsync(Flowchart flowchart)
    {
        return await new ActivityTestFixture(flowchart).ExecuteAsync();
    }
}
