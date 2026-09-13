using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Activities;

/// <summary>
/// Integration tests for the <see cref="Elsa.Workflows.Activities.Sequence"/> activity.
/// </summary>
public class SequenceTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    [Test]
    [DisplayName("Sequence executes all activities in order")]
    public async Task Sequence_ExecutesAllActivitiesInOrder()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SimpleSequenceWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines).IsEquivalentTo(new[] { "Activity 1", "Activity 2", "Activity 3" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Sequence with Break stops execution")]
    public async Task Sequence_WithBreak_StopsExecution()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SequenceWithBreakWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines).IsEquivalentTo(new[] { "Before Break" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(lines).DoesNotContain("After Break (should not execute)");
    }

    [Test]
    [DisplayName("Sequence with conditional Break stops when condition is met")]
    public async Task Sequence_WithConditionalBreak_StopsWhenConditionMet()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SequenceWithConditionalBreakWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines).IsEquivalentTo(new[] { "Iteration 1", "Iteration 2" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(lines).DoesNotContain("Iteration 3 (should not execute)");
    }

    [Test]
    [DisplayName("Nested sequences with Break propagates to outer sequence")]
    public async Task NestedSequences_WithBreak_PropagatesToOuterSequence()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new NestedSequencesWithBreakWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines).IsEquivalentTo(new[]
        {
            "Outer: Start",
            "Inner: Activity 1"
        }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(lines).DoesNotContain("Inner: Activity 2 (should not execute)");
        await Assert.That(lines).DoesNotContain("Outer: After inner sequence");
        await Assert.That(lines).DoesNotContain("Outer: End");
    }

    [Test]
    [DisplayName("Sequence with Complete terminal node stops workflow")]
    public async Task Sequence_WithComplete_StopsWorkflow()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SequenceWithCompleteWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines).IsEquivalentTo(new[] { "Before Complete" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(lines).DoesNotContain("After Complete (should not execute)");
    }

    [Test]
    [DisplayName("Sequence with variables makes them available to activities")]
    public async Task Sequence_WithVariables_MakesThemAvailableToActivities()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new SequenceWithVariablesWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines.Count).IsEqualTo(2);
        await Assert.That(lines[0]).Contains("Counter: 0, Name: Initial", StringComparison.CurrentCulture);
        await Assert.That(lines[1]).Contains("Counter: 10, Name: Updated", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("Empty sequence completes successfully")]
    public async Task EmptySequence_CompletesSuccessfully()
    {
        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(new EmptySequenceWorkflow());

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines).IsEmpty();
    }

    [Test]
    [DisplayName("Sequence executes different numbers of activities: $activityCount")]
    [Arguments(1)]
    [Arguments(5)]
    [Arguments(10)]
    public async Task Sequence_ExecutesDifferentNumbersOfActivities(int activityCount)
    {
        // Arrange
        var workflow = new DynamicSequenceWorkflow(activityCount);

        // Act
        var (result, lines) = await RunWorkflowAndCaptureOutput(workflow);

        // Assert
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(lines.Count).IsEqualTo(activityCount);
        for (var i = 0; i < activityCount; i++)
        {
            await Assert.That(lines[i]).IsEqualTo($"Activity {i + 1}");
        }
    }
    
    private async Task<(RunWorkflowResult Result, List<string> Lines)> RunWorkflowAndCaptureOutput(IWorkflow workflow)
    {
        var result = await _fixture.RunWorkflowAsync(workflow);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        return (result, lines);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_fixture);
}
