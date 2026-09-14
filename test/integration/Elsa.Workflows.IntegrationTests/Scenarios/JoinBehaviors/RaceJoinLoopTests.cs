using Elsa.Testing.Shared;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors.Workflows;
using Elsa.Workflows.Options;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors;

public class RaceJoinLoopTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "WaitAny join re-entered via loop can be triggered from another inbound")]
    public async Task WaitAny_join_reentered_via_loop_can_be_triggered_from_another_inbound()
    {
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        var result = await _fixture.RunWorkflowAsync<RaceJoinLoopWorkflow>(options);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "A", "Body", "Retry", "Body", "B", "Body", "End" }, lines);
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
    }
}
