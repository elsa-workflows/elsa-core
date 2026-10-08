using Elsa.Activities.IntegrationTests.Flow.Workflows;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;

namespace Elsa.Activities.IntegrationTests.Flow;

/// <summary>
/// Integration tests for the <see cref="Start"/> activity.
/// </summary>
public class StartTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new WorkflowTestFixture(testOutputHelper)
        .AddWorkflow<StartInSequenceWorkflow>()
        .AddWorkflow<StartInFlowchartAsExplicitStartWorkflow>()
        .AddWorkflow<StartInFlowchartWithStartPropertyWorkflow>();

    [Fact(DisplayName = "Start completes successfully in sequence")]
    public async Task Start_CompletesSuccessfullyInSequence()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(StartInSequenceWorkflow.DefinitionId);

        // Assert
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Contains("Before Start", lines);
        Assert.Contains("After Start", lines);
    }

    [Fact(DisplayName = "Start activity is used as flowchart start when present")]
    public async Task Start_IsUsedAsFlowchartStart_WhenPresent()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(StartInFlowchartAsExplicitStartWorkflow.DefinitionId);

        // Assert
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Contains("Start activity", lines);
        Assert.Contains("After Start", lines);
        Assert.DoesNotContain("Should not execute", lines);
    }

    [Fact(DisplayName = "Flowchart Start property takes precedence over explicit Start activity")]
    public async Task FlowchartStartProperty_TakesPrecedenceOverExplicitStartActivity()
    {
        // Act
        var workflowState = await _fixture.RunWorkflowAsync(StartInFlowchartWithStartPropertyWorkflow.DefinitionId);

        // Assert
        Assert.Equal(WorkflowStatus.Finished, workflowState.Status);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Contains("Start property used", lines);
        Assert.DoesNotContain("Start activity", lines);
    }
}
