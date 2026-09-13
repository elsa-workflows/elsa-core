using Elsa.Testing.Shared;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors.Workflows;
using Elsa.Workflows.Options;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors;

public class RaceJoinLoopTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    [Test]
    [DisplayName("WaitAny join re-entered via loop can be triggered from another inbound")]
    public async Task WaitAny_join_reentered_via_loop_can_be_triggered_from_another_inbound()
    {
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        var result = await _fixture.RunWorkflowAsync<RaceJoinLoopWorkflow>(options);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Start", "A", "Body", "Retry", "Body", "B", "Body", "End" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_fixture);
}
