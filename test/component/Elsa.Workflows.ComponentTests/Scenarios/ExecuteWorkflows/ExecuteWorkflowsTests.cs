using Elsa.Common.Models;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.ExecuteWorkflows.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.ExecuteWorkflows;

public class DispatchWorkflowsTests : AppComponentTest
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public DispatchWorkflowsTests(App app) : base(app)
    {
        _workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
    }

    [Fact]
    public async Task ExecuteWorkflow_ShouldExecuteWorkflow()
    {
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(MainWorkflow.DefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
    }
}