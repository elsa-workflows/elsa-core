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
    [InlineData(FlowchartExecutionMode.TokenBased, FlowJoinMode.WaitAny)]
    [InlineData(FlowchartExecutionMode.TokenBased, FlowJoinMode.WaitAll)]
    [InlineData(FlowchartExecutionMode.CounterBased, FlowJoinMode.WaitAny)]
    [InlineData(FlowchartExecutionMode.CounterBased, FlowJoinMode.WaitAll)]
    public async Task Should_Complete_In_All_Flow_Mode_Combinations(FlowchartExecutionMode executionMode, FlowJoinMode joinMode)
    {
        // Arrange & Act
        var context = await ExecuteWithFlowModeAsync(executionMode, joinMode);

        // Assert
        Assert.True(context.IsCompleted);
    }

    [Theory]
    [InlineData(FlowchartExecutionMode.TokenBased, FlowJoinMode.WaitAny)]
    [InlineData(FlowchartExecutionMode.TokenBased, FlowJoinMode.WaitAll)]
    [InlineData(FlowchartExecutionMode.CounterBased, FlowJoinMode.WaitAny)]
    [InlineData(FlowchartExecutionMode.CounterBased, FlowJoinMode.WaitAll)]
    public async Task Should_Execute_In_Flowchart_Context_For_All_Combinations(FlowchartExecutionMode executionMode, FlowJoinMode joinMode)
    {
        // Arrange & Act
        var (context, flowJoin) = await ExecuteInFlowchartWithFlowModeAsync(executionMode, joinMode);

        // Assert
        Assert.NotNull(context);
        
        var expectedMessage = executionMode == FlowchartExecutionMode.TokenBased
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
        var tokenContext = await ExecuteWithFlowModeAsync(FlowchartExecutionMode.TokenBased, joinMode);
        var counterContext = await ExecuteWithFlowModeAsync(FlowchartExecutionMode.CounterBased, joinMode);
        
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
    /// Executes a FlowJoin activity with the specified flow mode.
    /// </summary>
    private static async Task<ActivityExecutionContext> ExecuteWithFlowModeAsync(FlowchartExecutionMode executionMode, FlowJoinMode joinMode)
    {
        var flowJoin = CreateFlowJoin(joinMode);
        return await ExecuteAsync(flowJoin, executionMode);
    }

    /// <summary>
    /// Executes a FlowJoin activity within a flowchart context with the specified flow mode.
    /// </summary>
    private static async Task<(ActivityExecutionContext context, FlowJoin flowJoin)> ExecuteInFlowchartWithFlowModeAsync(FlowchartExecutionMode executionMode, FlowJoinMode joinMode)
    {
        var flowJoin = CreateFlowJoin(joinMode);
        var flowchart = CreateSimpleFlowchart(flowJoin);
        var context = await ExecuteFlowchartAsync(flowchart, executionMode);
        return (context, flowJoin);
    }
    
    /// <summary>
    /// Executes a flowchart using the ActivityTestFixture with the specified execution mode.
    /// </summary>
    private static Task<ActivityExecutionContext> ExecuteFlowchartAsync(Flowchart flowchart, FlowchartExecutionMode? executionMode = null)
    {
        return ExecuteAsync(flowchart, executionMode);
    }

    /// <summary>
    /// Executes an activity using the ActivityTestFixture with the specified execution mode.
    /// </summary>
    private static async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity, FlowchartExecutionMode? executionMode = null)
    {
        var fixture = new ActivityTestFixture(activity);

        if (executionMode.HasValue)
        {
            fixture.ConfigureContext(context =>
            {
                context.WorkflowExecutionContext.Properties[Flowchart.ExecutionModePropertyKey] = executionMode.Value;
            });
        }

        return await fixture.ExecuteAsync();
    }
}
