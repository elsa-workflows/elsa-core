using Elsa.Activities.IntegrationTests.Flow.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Flow;

/// <summary>
/// Integration tests for the <see cref="End"/> activity.
/// </summary>
public class EndTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(testOutputHelper)
        .AddWorkflow<EndInSequenceWorkflow>()
        .AddWorkflow<EndInFlowchartWorkflow>();

    [Fact(DisplayName = "End terminates sequence execution")]
    public async Task End_TerminatesSequenceExecution()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(EndInSequenceWorkflow.DefinitionId);

        // Assert
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Contains("Before End", lines);
        Assert.DoesNotContain("After End", lines);
    }

    [Fact(DisplayName = "End in flowchart terminates flowchart immediately")]
    public async Task End_InFlowchart_TerminatesFlowchartImmediately()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(EndInFlowchartWorkflow.DefinitionId);

        // Assert
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Contains("Start", lines);
        Assert.Contains("Path A executed", lines);
        // End is a terminal node - it terminates the flowchart immediately
        // Path B should not execute because End completes the flowchart
        Assert.DoesNotContain("Path B executed", lines);
        // The outer sequence continues after the flowchart completes
        Assert.Contains("After flowchart", lines);
    }
}
