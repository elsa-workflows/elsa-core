using Elsa.Testing.Shared;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Options;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors;

public class ForkDecisionJoinTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    [Test]
    [DisplayName("The implicit join configured with Stream merge mode should execute.")]
    public async Task ImplicitJoinStreamShouldExecute()
    {
        await RunAndAssert("fork-decision-join-none.json", ["A", "C"]);
    }

    [Test]
    [DisplayName("The implicit join configured with Merge mode should not execute (waits for activated branches only).")]
    public async Task ImplicitJoinMergeShouldNotExecute()
    {
        await RunAndAssert("fork-decision-join-converge.json", ["A"]);
    }

    [Test]
    [DisplayName("The implicit join configured with Converge mode should block (strictest - requires ALL inbound connections).")]
    public async Task ImplicitJoinConvergeShouldBlock()
    {
        await RunAndAssert("fork-decision-join-converge-strict.json", ["A"]);
    }

    [Test]
    [DisplayName("The explicit join configured with WaitAll join mode should block.")]
    public async Task ExplicitJoinWaitAllShouldBlock()
    {
        await RunAndAssert("fork-decision-join-waitall.json", ["A", "C", "B", "D"]);
    }

    [Test]
    [DisplayName("An implicit join from the True and False branches should execute because the join mode is Stream and by default, all active branches are joined.")]
    public async Task ImplicitJoinFromBranchesShouldExecute()
    {
        // Import workflow.
        var workflowDefinition = await _fixture.ImportWorkflowDefinitionAsync($"Scenarios/JoinBehaviors/Workflows/decision-merge-join-none.json");

        // Execute with token-based mode.
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        var workflowState = await _fixture.RunWorkflowAsync(workflowDefinition.DefinitionId, options: options);

        // Assert.
        await Assert.That(workflowState.Status).IsEqualTo(WorkflowStatus.Finished);
    }

    private async Task RunAndAssert(string workflowFileName, string[] expectedLines)
    {
        // Import workflow.
        var workflowDefinition = await _fixture.ImportWorkflowDefinitionAsync($"Scenarios/JoinBehaviors/Workflows/{workflowFileName}");

        // Execute with token-based mode.
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        await _fixture.RunWorkflowAsync(workflowDefinition.DefinitionId, options: options);

        // Assert.
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(expectedLines, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_fixture);
}
