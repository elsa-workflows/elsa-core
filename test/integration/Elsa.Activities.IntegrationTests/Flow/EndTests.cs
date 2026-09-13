using Elsa.Activities.IntegrationTests.Flow.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace Elsa.Activities.IntegrationTests.Flow;

/// <summary>
/// Integration tests for the <see cref="End"/> activity.
/// </summary>
public class EndTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(TestContext.Current!.Output.StandardOutput)
        .AddWorkflow<EndInSequenceWorkflow>()
        .AddWorkflow<EndInFlowchartWorkflow>();

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("End terminates sequence execution")]
    public async Task End_TerminatesSequenceExecution()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(EndInSequenceWorkflow.DefinitionId);

        // Assert
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Before End");

        await Assert.That(lines).DoesNotContain("After End");

    }

    [Test]
    [DisplayName("End in flowchart terminates flowchart immediately")]
    public async Task End_InFlowchart_TerminatesFlowchartImmediately()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(EndInFlowchartWorkflow.DefinitionId);

        // Assert
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);

        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).Contains("Start");

        await Assert.That(lines).Contains("Path A executed");

        // End is a terminal node - it terminates the flowchart immediately
        // Path B should not execute because End completes the flowchart
        await Assert.That(lines).DoesNotContain("Path B executed");

        // The outer sequence continues after the flowchart completes
        await Assert.That(lines).Contains("After flowchart");

    }
}
