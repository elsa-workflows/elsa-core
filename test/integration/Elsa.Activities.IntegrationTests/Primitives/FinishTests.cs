using Elsa.Activities.IntegrationTests.Primitives.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.State;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Primitives;

/// <summary>
/// Integration tests for the <see cref="Finish"/> activity.
/// </summary>
public class FinishTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(testOutputHelper)
        .AddWorkflow<FinishInSequenceWorkflow>();

    [Fact(DisplayName = "Finish terminates workflow and prevents subsequent activities from executing")]
    public async Task Finish_TerminatesWorkflowAndPreventsSubsequentActivities()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(FinishInSequenceWorkflow.DefinitionId);

        // Assert
        AssertWorkflowFinished(workflowState);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Contains("Before Finish", lines);
        Assert.DoesNotContain("After Finish", lines);
    }

    private static void AssertWorkflowFinished(WorkflowState workflowState)
    {
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        Assert.Equal(WorkflowSubStatus.Finished, workflowState.SubStatus);
    }
}
