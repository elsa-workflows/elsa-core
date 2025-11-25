using Elsa.Testing.Shared;
using Elsa.Workflows.Activities.Flowchart.Activities;
using static Elsa.Activities.UnitTests.Flow.FlowchartTestHelpers;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Tests for common Flowchart behavior (both counter and token-based strategies).
/// </summary>
public class FlowchartTests
{
    [Fact(DisplayName = "Schedules start activity when specified")]
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
        Assert.True(context.HasScheduledActivity(startActivity));
    }

    [Fact(DisplayName = "Executes without error when no start activity specified")]
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
        Assert.NotNull(context);
        Assert.False(context.HasScheduledActivity(new WriteLine("NonExistent")));
    }
    
    [Theory(DisplayName = "Respects UseTokenFlow flag")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RespectsUseTokenFlowFlag(bool useTokenFlow)
    {
        // Arrange
        var originalValue = Flowchart.UseTokenFlow;
        Flowchart.UseTokenFlow = useTokenFlow;

        try
        {
            var activity = new WriteLine("Test");
            var flowchart = new Flowchart
            {
                Start = activity,
                Activities = { activity }
            };

            // Act
            var context = await ExecuteFlowchartAsync(flowchart);

            // Assert - just verify it executes without error
            Assert.NotNull(context);
        }
        finally
        {
            Flowchart.UseTokenFlow = originalValue;
        }
    }

    [Fact(DisplayName = "Accepts empty connections collection")]
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
        Assert.True(context.HasScheduledActivity(activity));
    }
}
