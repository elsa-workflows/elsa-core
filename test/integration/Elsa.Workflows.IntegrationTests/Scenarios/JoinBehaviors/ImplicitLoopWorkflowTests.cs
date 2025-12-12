using Elsa.Testing.Shared;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors.Workflows;
using Elsa.Workflows.Options;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.JoinBehaviors;

public class ImplicitWorkflowTests(ITestOutputHelper testOutputHelper)
{
    private readonly WorkflowTestFixture _fixture = new(testOutputHelper);

    [Fact(DisplayName = "Implicit loop workflows are executed correctly")]
    public async Task Test1()
    {
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        await _fixture.RunWorkflowAsync<ImplicitLoopWorkflow>(options);
        var lines = _fixture.CapturingTextWriter.Lines.ToList();
        Assert.Equal(new[] { "Start", "Retry", "End" }, lines);
    }

    [Fact(DisplayName = "Implicit loop workflows complete the workflow")]
    public async Task Test2()
    {
        var options = new RunWorkflowOptions().WithTokenBasedFlowchart();
        var result = await _fixture.RunWorkflowAsync<ImplicitLoopWorkflow>(options);
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
    }
}