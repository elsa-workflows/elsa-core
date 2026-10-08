using Elsa.Testing.Shared;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors.Workflows;
using Elsa.Workflows.Options;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors;

public class BraidedWorkflowTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "Braided workflows are executed correctly")]
    public async Task Test1()
    {
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        await _fixture.RunWorkflowAsync<BraidedWorkflow>(options);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "WriteLine1", "WriteLine2", "WriteLine3", "WriteLine4", "WriteLine5", "WriteLine6", "WriteLine7" }, lines);
    }

    [Fact(DisplayName = "Braided workflows complete the workflow")]
    public async Task Test2()
    {
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        var result = await _fixture.RunWorkflowAsync<BraidedWorkflow>(options);
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
    }
}