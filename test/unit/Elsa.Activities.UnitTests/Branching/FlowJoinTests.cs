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
    [InlineData(FlowJoinMode.WaitAny)]
    [InlineData(FlowJoinMode.WaitAll)]
    public async Task Should_Complete_Activity_In_Token_Flow_Mode(FlowJoinMode joinMode)
    {
        // Arrange
        var originalValue = Flowchart.UseTokenFlow;
        Flowchart.UseTokenFlow = true;

        try
        {
            var flowJoin = new FlowJoin { Mode = new(joinMode) };

            // Act
            var context = await ExecuteAsync(flowJoin);

            // Assert
            Assert.True(context.IsCompleted);
        }
        finally
        {
            Flowchart.UseTokenFlow = originalValue;
        }
    }

    [Theory]
    [InlineData(FlowJoinMode.WaitAny)]
    [InlineData(FlowJoinMode.WaitAll)]
    public async Task Should_Complete_Activity_In_Counter_Flow_Mode(FlowJoinMode joinMode)
    {
        // Arrange  
        var originalValue = Flowchart.UseTokenFlow;
        Flowchart.UseTokenFlow = false;

        try
        {
            var flowJoin = new FlowJoin { Mode = new(joinMode) };

            // Act - Execute without parent context (simple case)
            var context = await ExecuteAsync(flowJoin);

            // Assert
            Assert.True(context.IsCompleted);
        }
        finally
        {
            Flowchart.UseTokenFlow = originalValue;
        }
    }

    [Fact]
    public async Task Should_Execute_In_Flowchart_Context()
    {
        // Arrange
        var originalValue = Flowchart.UseTokenFlow;
        Flowchart.UseTokenFlow = false;

        try
        {
            var flowJoin = new FlowJoin { Mode = new(FlowJoinMode.WaitAny) };
            var flowchart = new Flowchart
            {
                Start = flowJoin,
                Activities = { flowJoin }
            };

            // Act
            var context = await ExecuteFlowchartAsync(flowchart);

            // Assert
            Assert.NotNull(context);
            Assert.True(context.HasScheduledActivity(flowJoin));
        }
        finally
        {
            Flowchart.UseTokenFlow = originalValue;
        }
    }

    // Comprehensive tests covering all flow mode combinations
    [Theory]
    [InlineData(true, FlowJoinMode.WaitAny)]
    [InlineData(true, FlowJoinMode.WaitAll)]
    [InlineData(false, FlowJoinMode.WaitAny)]
    [InlineData(false, FlowJoinMode.WaitAll)]
    public async Task Should_Complete_In_All_Flow_Mode_Combinations(bool useTokenFlow, FlowJoinMode joinMode)
    {
        // Arrange
        var originalValue = Flowchart.UseTokenFlow;
        Flowchart.UseTokenFlow = useTokenFlow;

        try
        {
            var flowJoin = new FlowJoin { Mode = new(joinMode) };

            // Act
            var context = await ExecuteAsync(flowJoin);

            // Assert
            Assert.True(context.IsCompleted);
            
            // Document expected behavior based on flow mode
            if (useTokenFlow)
            {
                // In token flow mode, FlowJoin acts as a no-op and always completes
                // regardless of join mode (WaitAny or WaitAll)
                Assert.True(context.IsCompleted, "Token flow mode should always complete");
            }
            else
            {
                // In counter flow mode, behavior depends on join mode and flowchart state
                // Without parent context, both WaitAny and WaitAll should complete
                Assert.True(context.IsCompleted, "Counter flow mode should complete when no parent context");
            }
        }
        finally
        {
            Flowchart.UseTokenFlow = originalValue;
        }
    }

    [Theory]
    [InlineData(true, FlowJoinMode.WaitAny)]
    [InlineData(true, FlowJoinMode.WaitAll)]
    [InlineData(false, FlowJoinMode.WaitAny)]
    [InlineData(false, FlowJoinMode.WaitAll)]
    public async Task Should_Execute_In_Flowchart_Context_For_All_Combinations(bool useTokenFlow, FlowJoinMode joinMode)
    {
        // Test all combinations within a flowchart context
        // Arrange
        var originalValue = Flowchart.UseTokenFlow;
        Flowchart.UseTokenFlow = useTokenFlow;

        try
        {
            var flowJoin = new FlowJoin { Mode = new(joinMode) };
            var flowchart = new Flowchart
            {
                Start = flowJoin,
                Activities = { flowJoin }
            };

            // Act
            var context = await ExecuteFlowchartAsync(flowchart);

            // Assert
            Assert.NotNull(context);
            
            if (useTokenFlow)
            {
                // In token flow mode, the join should be scheduled regardless of join mode
                Assert.True(context.HasScheduledActivity(flowJoin), 
                    $"Token flow mode with {joinMode} should schedule the join activity");
            }
            else
            {
                // In counter flow mode, scheduling depends on join mode and flowchart state
                // Since this is a simple flowchart with just the join as start activity,
                // it should be scheduled
                Assert.True(context.HasScheduledActivity(flowJoin), 
                    $"Counter flow mode with {joinMode} should schedule the join activity as start");
            }
        }
        finally
        {
            Flowchart.UseTokenFlow = originalValue;
        }
    }

    [Theory]
    [InlineData(true, FlowJoinMode.WaitAny)]
    [InlineData(true, FlowJoinMode.WaitAll)]
    [InlineData(false, FlowJoinMode.WaitAny)]
    [InlineData(false, FlowJoinMode.WaitAll)]
    public async Task Should_Handle_Complex_Flowchart_Scenarios(bool useTokenFlow, FlowJoinMode joinMode)
    {
        // Test with a more complex flowchart that has multiple activities
        // Arrange
        var originalValue = Flowchart.UseTokenFlow;
        Flowchart.UseTokenFlow = useTokenFlow;

        try
        {
            var startActivity = new WriteLine("Start");
            var flowJoin = new FlowJoin { Mode = new(joinMode) };
            var afterJoin = new WriteLine("AfterJoin");
            
            var flowchart = new Flowchart
            {
                Start = startActivity,
                Activities = { startActivity, flowJoin, afterJoin },
                Connections = 
                {
                    new() { Source = new(startActivity, "Done"), Target = new(flowJoin) },
                    new() { Source = new(flowJoin, "Done"), Target = new(afterJoin) }
                }
            };

            // Act
            var context = await ExecuteFlowchartAsync(flowchart);

            // Assert
            Assert.NotNull(context);
            
            // The start activity should always be scheduled first
            Assert.True(context.HasScheduledActivity(startActivity), 
                "Start activity should be scheduled");
            
            // Document expected behavior for different combinations
            if (useTokenFlow)
            {
                // In token flow mode, both WaitAny and WaitAll act as no-ops
                // The exact scheduling depends on the flowchart execution engine
            }
            else
            {
                // In counter flow mode:
                // - WaitAny should allow continuation after first inbound connection
                // - WaitAll should wait for all inbound connections (in this case just one)
                // Since there's only one inbound connection, both should behave similarly
            }
        }
        finally
        {
            Flowchart.UseTokenFlow = originalValue;
        }
    }

    [Fact]
    public async Task Should_Demonstrate_Token_vs_Counter_Flow_Differences()
    {
        // This test demonstrates the key differences between token and counter flow modes
        var joinMode = FlowJoinMode.WaitAll; // Use WaitAll to highlight differences
        
        // Test Token Flow Mode
        var originalValue = Flowchart.UseTokenFlow;
        Flowchart.UseTokenFlow = true;
        
        try
        {
            var tokenFlowJoin = new FlowJoin { Mode = new(joinMode) };
            var tokenContext = await ExecuteAsync(tokenFlowJoin);
            
            // Token flow: FlowJoin acts as no-op, always completes
            Assert.True(tokenContext.IsCompleted, "Token flow should always complete");
        }
        finally
        {
            Flowchart.UseTokenFlow = originalValue;
        }
        
        // Test Counter Flow Mode
        Flowchart.UseTokenFlow = false;
        
        try
        {
            var counterFlowJoin = new FlowJoin { Mode = new(joinMode) };
            var counterContext = await ExecuteAsync(counterFlowJoin);
            
            // Counter flow: Behavior depends on CanWaitAllProceed for WaitAll mode
            // Without parent context, it should complete
            Assert.True(counterContext.IsCompleted, "Counter flow should complete without parent context");
        }
        finally
        {
            Flowchart.UseTokenFlow = originalValue;
        }
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(IActivity activity)
    {
        return await new ActivityTestFixture(activity).ExecuteAsync();
    }

    private static async Task<ActivityExecutionContext> ExecuteFlowchartAsync(Flowchart flowchart)
    {
        return await new ActivityTestFixture(flowchart).ExecuteAsync();
    }
}
