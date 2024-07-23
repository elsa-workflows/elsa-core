﻿using Elsa.Workflows.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.FlowJoins;

public class Tests(App app) : AppComponentTest(app)
{
    // https://github.com/elsa-workflows/elsa-core/issues/5348
    [Fact]
    public async Task FlowchartWithSingleFlowJoin_ShouldExecuteSuccessfully()
    {
        var workflowRunner = Scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var result = await workflowRunner.RunAsync<SingleJoinWorkflow>();
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }
}