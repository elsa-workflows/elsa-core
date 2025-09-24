using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.DispatchWorkflows.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.DispatchWorkflows;

public class DispatchWorkflowsTests : AppComponentTest
{
    private readonly SignalManager _signalManager;
    private readonly IWorkflowRuntime _workflowRuntime;

    public DispatchWorkflowsTests(App app) : base(app)
    {
        _workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        _signalManager = Scope.ServiceProvider.GetRequiredService<SignalManager>();
    }

    [Fact (Skip = "This test is flaky and needs to be fixed.")]
    public async Task DispatchAndWaitWorkflow_ShouldWaitForChildWorkflowToComplete()
    {
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(DispatchAndWaitWorkflow.DefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
        await _signalManager.WaitAsync<object>("Completed");
    }
}