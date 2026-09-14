using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Branching.FlowJoin;

public class Tests(App app) : AppComponentTest(app)
{
    // https://github.com/elsa-workflows/elsa-core/issues/5348
    [Test]
    public async Task FlowchartWithSingleFlowJoin_ShouldExecuteSuccessfully()
    {
        var workflowRunner = Scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var result = await workflowRunner.RunAsync<SingleJoinWorkflow>();
        await Assert.That(result.WorkflowState.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);
    }
}