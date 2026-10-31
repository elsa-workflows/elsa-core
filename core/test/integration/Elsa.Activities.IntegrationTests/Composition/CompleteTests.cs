using Elsa.Activities.IntegrationTests.Composition.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Composition;

/// <summary>
/// Integration tests for the <see cref="Complete"/> activity.
/// </summary>
public class CompleteTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(testOutputHelper)
        .AddWorkflow<CompleteTerminatesCompositeWorkflow>()
        .AddWorkflow<CompleteInNestedCompositeWorkflow>();

    [Fact(DisplayName = "Complete terminates composite execution immediately")]
    public async Task Complete_TerminatesCompositeExecutionImmediately()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(CompleteTerminatesCompositeWorkflow.DefinitionId);

        // Assert
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Contains("Before Complete", lines);
        Assert.DoesNotContain("This should not execute", lines);
        Assert.Contains("After composite", lines);
    }

    [Fact(DisplayName = "Complete in nested composite completes immediate parent only")]
    public async Task Complete_InNestedComposite_CompletesImmediateParentOnly()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(CompleteInNestedCompositeWorkflow.DefinitionId);

        // Assert
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Contains("Outer composite started", lines);
        Assert.Contains("Inner composite started", lines);
        Assert.Contains("Inner composite completed", lines);
        Assert.Contains("Outer composite completed", lines);
        Assert.DoesNotContain("This should not execute in inner", lines);
    }
}