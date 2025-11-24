using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Activities.SequenceActivity;

/// <summary>
/// Integration tests for the <see cref="Workflows.Activities.Sequence"/> activity.
/// </summary>
public class SequenceTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    private async Task<(RunWorkflowResult Result, List<string> Lines)> RunWorkflowAndCaptureOutput(IWorkflow workflow)
    {
        var result = await _fixture.RunWorkflowAsync(workflow);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        return (result, lines);
    }

    [Fact(DisplayName = "Sequence executes all activities in order")]
    public async Task Sequence_ExecutesAllActivitiesInOrder()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SimpleSequenceWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(new[] { "Activity 1", "Activity 2", "Activity 3" }, lines);
    }

    [Fact(DisplayName = "Sequence with Break stops execution")]
    public async Task Sequence_WithBreak_StopsExecution()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SequenceWithBreakWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(new[] { "Before Break" }, lines);
        Assert.DoesNotContain("After Break (should not execute)", lines);
    }

    [Fact(DisplayName = "Sequence with conditional Break stops when condition is met")]
    public async Task Sequence_WithConditionalBreak_StopsWhenConditionMet()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SequenceWithConditionalBreakWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(new[] { "Iteration 1", "Iteration 2" }, lines);
        Assert.DoesNotContain("Iteration 3 (should not execute)", lines);
    }

    [Fact(DisplayName = "Nested sequences with Break propagates to outer sequence")]
    public async Task NestedSequences_WithBreak_PropagatesToOuterSequence()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new NestedSequencesWithBreakWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(new[]
        {
            "Outer: Start",
            "Inner: Activity 1"
        }, lines);
        Assert.DoesNotContain("Inner: Activity 2 (should not execute)", lines);
        Assert.DoesNotContain("Outer: After inner sequence", lines);
        Assert.DoesNotContain("Outer: End", lines);
    }

    [Fact(DisplayName = "Sequence with Complete terminal node stops workflow")]
    public async Task Sequence_WithComplete_StopsWorkflow()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SequenceWithCompleteWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(new[] { "Before Complete" }, lines);
        Assert.DoesNotContain("After Complete (should not execute)", lines);
    }

    [Fact(DisplayName = "Sequence with variables makes them available to activities")]
    public async Task Sequence_WithVariables_MakesThemAvailableToActivities()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SequenceWithVariablesWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(2, lines.Count);
        Assert.Contains("Counter: 0, Name: Initial", lines[0]);
        Assert.Contains("Counter: 10, Name: Updated", lines[1]);
    }

    [Fact(DisplayName = "Empty sequence completes successfully")]
    public async Task EmptySequence_CompletesSuccessfully()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new EmptySequenceWorkflow());

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Empty(lines);
    }

    [Theory(DisplayName = "Sequence executes different numbers of activities")]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Sequence_ExecutesDifferentNumbersOfActivities(int activityCount)
    {
        // Arrange
        var workflow = new DynamicSequenceWorkflow(activityCount);

        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(workflow);

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Equal(activityCount, lines.Count);
        for (var i = 0; i < activityCount; i++)
        {
            Assert.Equal($"Activity {i + 1}", lines[i]);
        }
    }
}

/// <summary>
/// Workflow with dynamic number of activities in sequence.
/// </summary>
public class DynamicSequenceWorkflow(int activityCount) : WorkflowBase
{
    protected override void Build(IWorkflowBuilder workflow)
    {
        var sequence = new Sequence();

        for (var i = 1; i <= activityCount; i++)
        {
            sequence.Activities.Add(new WriteLine($"Activity {i}"));
        }

        workflow.Root = sequence;
    }
}
