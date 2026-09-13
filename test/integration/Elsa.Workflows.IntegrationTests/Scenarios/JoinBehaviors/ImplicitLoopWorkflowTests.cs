using Elsa.Testing.Shared;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors.Workflows;
using Elsa.Workflows.Options;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors;

public class ImplicitWorkflowTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    [Test]
    [DisplayName("Implicit loop workflows are executed correctly")]
    public async Task Test1()
    {
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        await _fixture.RunWorkflowAsync<ImplicitLoopWorkflow>(options);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        await Assert.That(lines).IsEquivalentTo(new[] { "Start", "Retry", "End" }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    [DisplayName("Implicit loop workflows complete the workflow")]
    public async Task Test2()
    {
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        var result = await _fixture.RunWorkflowAsync<ImplicitLoopWorkflow>(options);
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
    }

    public ValueTask DisposeAsync() => TestResourceDisposal.DisposeAsync(_fixture);
}
