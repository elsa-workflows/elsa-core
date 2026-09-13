using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using static Elsa.Activities.IntegrationTests.Flow.FlowchartTestHelpers;

namespace Elsa.Activities.IntegrationTests.Branching;

/// <summary>
/// Integration tests for FlowJoin activity in complex flowchart scenarios.
/// </summary>
public class FlowJoinTests : IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly CapturingTextWriter _output;

    public FlowJoinTests()
    {
        _output = new();
        _services = CreateServiceProvider(TestContext.Current!.Output.StandardOutput, _output);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeServiceProviderAsync(_services);
        _output.Dispose();
    }

    [Test]
    [Arguments(true, FlowJoinMode.WaitAny)]
    [Arguments(true, FlowJoinMode.WaitAll)]
    [Arguments(false, FlowJoinMode.WaitAny)]
    [Arguments(false, FlowJoinMode.WaitAll)]
    public async Task Should_Handle_Complex_Flowchart_Scenarios(bool useTokenFlow, FlowJoinMode joinMode)
    {
        // Test with a more complex flowchart that has multiple activities
        // Arrange
        var executionMode = useTokenFlow ? FlowchartExecutionMode.TokenBased : FlowchartExecutionMode.CounterBased;

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
        await RunFlowchartAsync(_services, flowchart, executionMode);

        // Assert
        await Assert.That(_output.Lines).Contains("Start");

        
      
        // In token flow mode, both WaitAny and WaitAll should act as no-ops
        // The flowchart should execute normally: Start -> FlowJoin -> AfterJoin
        
        // In counter flow mode:
        // - WaitAny should allow continuation after first inbound connection
        // - WaitAll should wait for all inbound connections (in this case just one)
        
        // Since there's only one inbound connection, both should behave similarly
        
        await Assert.That(_output.Lines).Contains("AfterJoin");

    }

    [Test]
    [Arguments(true, FlowJoinMode.WaitAny)]
    [Arguments(true, FlowJoinMode.WaitAll)]
    [Arguments(false, FlowJoinMode.WaitAny)]
    [Arguments(false, FlowJoinMode.WaitAll)]
    public async Task Should_Handle_Fork_Join_Scenarios(bool useTokenFlow, FlowJoinMode joinMode)
    {
        // Test FlowJoin with a Fork-Join pattern
        // Arrange
        var executionMode = useTokenFlow ? FlowchartExecutionMode.TokenBased : FlowchartExecutionMode.CounterBased;

        var start = new WriteLine("Start");
        var fork = new FlowFork { Branches = new(["Branch1", "Branch2"]) };
        var branch1 = new WriteLine("Branch1");
        var branch2 = new WriteLine("Branch2");
        var join = new FlowJoin { Mode = new(joinMode) };
        var afterJoin = new WriteLine("AfterJoin");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, fork, branch1, branch2, join, afterJoin },
            Connections =
            {
                new() { Source = new(start, "Done"), Target = new(fork) },
                new() { Source = new(fork, "Branch1"), Target = new(branch1) },
                new() { Source = new(fork, "Branch2"), Target = new(branch2) },
                new() { Source = new(branch1, "Done"), Target = new(join) },
                new() { Source = new(branch2, "Done"), Target = new(join) },
                new() { Source = new(join, "Done"), Target = new(afterJoin) }
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, executionMode);

        // Assert
        await Assert.That(_output.Lines).Contains("Start");

        
        if (useTokenFlow)
        {
            // In token flow mode, FlowJoin acts as a no-op
            // Both branches should execute and continue to AfterJoin
            await Assert.That(_output.Lines).Contains("Branch1");

            await Assert.That(_output.Lines).Contains("Branch2");

            await Assert.That(_output.Lines).Contains("AfterJoin");

        }
        else
        {
            // In counter flow mode, behavior depends on join mode
            if (joinMode == FlowJoinMode.WaitAny)
            {
                // WaitAny allows continuation after first branch completes
                await Assert.That(_output.Lines).Contains("AfterJoin");

                // At least one branch should execute
                await Assert.That(_output.Lines.Contains("Branch1") || _output.Lines.Contains("Branch2")).IsTrue();

            }
            else // WaitAll
            {
                // WaitAll waits for both branches to complete
                await Assert.That(_output.Lines).Contains("Branch1");

                await Assert.That(_output.Lines).Contains("Branch2");

                await Assert.That(_output.Lines).Contains("AfterJoin");

            }
        }
    }

    [Test]
    [Arguments(true, FlowJoinMode.WaitAny)]
    [Arguments(true, FlowJoinMode.WaitAll)]
    [Arguments(false, FlowJoinMode.WaitAny)]
    [Arguments(false, FlowJoinMode.WaitAll)]
    public async Task Should_Handle_Multiple_Join_Scenarios(bool useTokenFlow, FlowJoinMode joinMode)
    {
        // Test multiple FlowJoin activities in a complex flowchart
        // Arrange
        var executionMode = useTokenFlow ? FlowchartExecutionMode.TokenBased : FlowchartExecutionMode.CounterBased;

        var start = new WriteLine("Start");
        var fork1 = new FlowFork { Branches = new(["A", "B"]) };
        var activityA = new WriteLine("A");
        var activityB = new WriteLine("B");
        var join1 = new FlowJoin { Mode = new(joinMode) };
        var middle = new WriteLine("Middle");
        var fork2 = new FlowFork { Branches = new(["C", "D"]) };
        var activityC = new WriteLine("C");
        var activityD = new WriteLine("D");
        var join2 = new FlowJoin { Mode = new(joinMode) };
        var end = new WriteLine("End");

        var flowchart = new Flowchart
        {
            Start = start,
            Activities = { start, fork1, activityA, activityB, join1, middle, fork2, activityC, activityD, join2, end },
            Connections =
            {
                new() { Source = new(start, "Done"), Target = new(fork1) },
                new() { Source = new(fork1, "A"), Target = new(activityA) },
                new() { Source = new(fork1, "B"), Target = new(activityB) },
                new() { Source = new(activityA, "Done"), Target = new(join1) },
                new() { Source = new(activityB, "Done"), Target = new(join1) },
                new() { Source = new(join1, "Done"), Target = new(middle) },
                new() { Source = new(middle, "Done"), Target = new(fork2) },
                new() { Source = new(fork2, "C"), Target = new(activityC) },
                new() { Source = new(fork2, "D"), Target = new(activityD) },
                new() { Source = new(activityC, "Done"), Target = new(join2) },
                new() { Source = new(activityD, "Done"), Target = new(join2) },
                new() { Source = new(join2, "Done"), Target = new(end) }
            }
        };

        // Act
        await RunFlowchartAsync(_services, flowchart, executionMode);

        // Assert
        await Assert.That(_output.Lines).Contains("Start");

        await Assert.That(_output.Lines).Contains("Middle");

        await Assert.That(_output.Lines).Contains("End");

        
        // Verify expected execution patterns based on flow mode and join mode
        if (useTokenFlow)
        {
            // In token flow mode, all activities should execute
            await Assert.That(_output.Lines).Contains("A");

            await Assert.That(_output.Lines).Contains("B");

            await Assert.That(_output.Lines).Contains("C");

            await Assert.That(_output.Lines).Contains("D");

        }
        else
        {
            // In counter flow mode, execution depends on join mode
            if (joinMode == FlowJoinMode.WaitAll)
            {
                // WaitAll ensures all parallel branches complete
                await Assert.That(_output.Lines).Contains("A");

                await Assert.That(_output.Lines).Contains("B");

                await Assert.That(_output.Lines).Contains("C");

                await Assert.That(_output.Lines).Contains("D");

            }
            else // WaitAny
            {
                // WaitAny allows continuation after first branch in each fork
                // At least one from each pair should execute
                await Assert.That(_output.Lines.Contains("A") || _output.Lines.Contains("B")).IsTrue();

                await Assert.That(_output.Lines.Contains("C") || _output.Lines.Contains("D")).IsTrue();

            }
        }
    }
}
