using Elsa.Activities.IntegrationTests.Composition.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Composition;

/// <summary>
/// Integration tests for the <see cref="Complete"/> activity.
/// </summary>
public class CompleteTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(TestContext.Current!.Output.StandardOutput)
        .AddWorkflow<CompleteTerminatesCompositeWorkflow>()
        .AddWorkflow<CompleteInNestedCompositeWorkflow>();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("Complete terminates composite execution immediately")]
    public async Task Complete_TerminatesCompositeExecutionImmediately()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(CompleteTerminatesCompositeWorkflow.DefinitionId);

        // Assert
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Before Complete");

        await Assert.That(lines).DoesNotContain("This should not execute");

        await Assert.That(lines).Contains("After composite");

    }

    [Test]
    [DisplayName("Complete in nested composite completes immediate parent only")]
    public async Task Complete_InNestedComposite_CompletesImmediateParentOnly()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(CompleteInNestedCompositeWorkflow.DefinitionId);

        // Assert
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Outer composite started");

        await Assert.That(lines).Contains("Inner composite started");

        await Assert.That(lines).Contains("Inner composite completed");

        await Assert.That(lines).Contains("Outer composite completed");

        await Assert.That(lines).DoesNotContain("This should not execute in inner");

    }
}
