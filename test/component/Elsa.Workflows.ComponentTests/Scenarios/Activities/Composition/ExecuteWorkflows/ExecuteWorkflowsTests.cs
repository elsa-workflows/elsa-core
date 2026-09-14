using Elsa.Common.Models;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.ExecuteWorkflows.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.ExecuteWorkflows;

public class ExecuteWorkflowsTests : AppComponentTest
{
    private IWorkflowRuntime _workflowRuntime = null!;

    public ExecuteWorkflowsTests(App app) : base(app)
    {
    }

    protected override ValueTask OnInitializeAsync()
    {
        _workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        return ValueTask.CompletedTask;
    }

    [Test]
    public async Task ExecuteWorkflow_ShouldExecuteWorkflow()
    {
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(MainWorkflow.DefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
    }
}
