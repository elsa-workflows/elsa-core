using Elsa.Testing.Shared;
using Elsa.Workflows.Activities.Flowchart.Activities;
using Elsa.Workflows.Activities.Flowchart.Models;
using static Elsa.Activities.UnitTests.Flow.FlowchartTestHelpers;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Tests for common Flowchart behavior (both counter and token-based strategies).
/// </summary>
public class FlowchartTests
{
    [Test]
    [DisplayName("Schedules start activity when specified")]
    public async Task SchedulesStartActivity()
    {
        // Arrange
        var startActivity = new WriteLine("Start");
        var flowchart = new Flowchart
        {
            Start = startActivity
        };

        // Act
        var context = await ExecuteFlowchartAsync(flowchart);

        // Assert
        await Assert.That(context.HasScheduledActivity(startActivity)).IsTrue();
    }

    [Test]
    [DisplayName("Executes without error when no start activity specified")]
    public async Task ExecutesWithoutErrorWhenNoStartActivity()
    {
        // Arrange
        var flowchart = new Flowchart
        {
            Start = null
        };

        // Act
        var context = await ExecuteFlowchartAsync(flowchart);

        // Assert
        await Assert.That(context).IsNotNull();
        await Assert.That(context.HasScheduledActivity(new WriteLine("NonExistent"))).IsFalse();
    }

    [Test]
    [DisplayName("Respects execution mode")]
    [Arguments(FlowchartExecutionMode.TokenBased)]
    [Arguments(FlowchartExecutionMode.CounterBased)]
    public async Task RespectsExecutionMode(FlowchartExecutionMode executionMode)
    {
        // Arrange
        var activity = new WriteLine("Test");
        var flowchart = new Flowchart
        {
            Start = activity,
            Activities = { activity }
        };

        // Act
        var context = await ExecuteFlowchartAsync(flowchart, executionMode);

        // Assert - just verify it executes without error
        await Assert.That(context).IsNotNull();
    }

    [Test]
    [DisplayName("Accepts empty connections collection")]
    public async Task AcceptsEmptyConnections()
    {
        // Arrange
        var activity = new WriteLine("Isolated");
        var flowchart = new Flowchart
        {
            Start = activity,
            Activities = { activity }
        };

        // Act
        var context = await ExecuteFlowchartAsync(flowchart);

        // Assert
        await Assert.That(context.HasScheduledActivity(activity)).IsTrue();
    }
}