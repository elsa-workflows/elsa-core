using Elsa.Activities.IntegrationTests.Flow.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Flow;

/// <summary>
/// Integration tests for the <see cref="Start"/> activity.
/// </summary>
public class StartTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(TestContext.Current!.Output.StandardOutput)
        .AddWorkflow<StartInSequenceWorkflow>()
        .AddWorkflow<StartInFlowchartAsExplicitStartWorkflow>()
        .AddWorkflow<StartInFlowchartWithStartPropertyWorkflow>();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("Start completes successfully in sequence")]
    public async Task Start_CompletesSuccessfullyInSequence()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(StartInSequenceWorkflow.DefinitionId);

        // Assert
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Before Start");

        await Assert.That(lines).Contains("After Start");

    }

    [Test]
    [DisplayName("Start activity is used as flowchart start when present")]
    public async Task Start_IsUsedAsFlowchartStart_WhenPresent()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(StartInFlowchartAsExplicitStartWorkflow.DefinitionId);

        // Assert
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Start activity");

        await Assert.That(lines).Contains("After Start");

        await Assert.That(lines).DoesNotContain("Should not execute");

    }

    [Test]
    [DisplayName("Flowchart Start property takes precedence over explicit Start activity")]
    public async Task FlowchartStartProperty_TakesPrecedenceOverExplicitStartActivity()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(StartInFlowchartWithStartPropertyWorkflow.DefinitionId);

        // Assert
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Start property used");

        await Assert.That(lines).DoesNotContain("Start activity");

    }
}
