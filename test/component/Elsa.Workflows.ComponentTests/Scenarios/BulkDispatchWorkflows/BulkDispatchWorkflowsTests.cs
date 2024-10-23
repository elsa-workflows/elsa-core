using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Helpers;
using Elsa.Workflows.ComponentTests.Scenarios.BulkDispatchWorkflows.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatchWorkflows;

public class BulkDispatchWorkflowsTests : AppComponentTest
{
    private readonly WorkflowEvents _workflowEvents;
    private readonly SignalManager _signalManager;
    private readonly IWorkflowRuntime _workflowRuntime;
    private static readonly object GreetEmployeesWorkflowCompletedSignal = new();

    public BulkDispatchWorkflowsTests(App app) : base(app)
    {
        _workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        _workflowEvents = Scope.ServiceProvider.GetRequiredService<WorkflowEvents>();
        _signalManager = Scope.ServiceProvider.GetRequiredService<SignalManager>();
        _workflowEvents.WorkflowInstanceSaved += OnWorkflowInstanceSaved;
    }

    /// Dispatches and waits for child workflows to complete.
    [Fact]
    public async Task DispatchAndWaitWorkflow_ShouldWaitForChildWorkflowToComplete()
    {
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(GreetEmployeesWorkflow.DefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
        var parentWorkflowInstanceArgs = await _signalManager.WaitAsync<WorkflowInstanceSavedEventArgs>(GreetEmployeesWorkflowCompletedSignal);

        Assert.Equal(WorkflowStatus.Finished, parentWorkflowInstanceArgs.WorkflowInstance.Status);
    }

    /// <summary>
    /// Individual items are sent as input to child workflows.
    /// </summary>
    [Fact]
    public async Task DispatchWorkflows_ChildWorkflowsShouldReceiveCurrentItem()
    {
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        var request = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(MixFruitsWorkflow.DefinitionId, VersionOptions.Published)
        };
        await workflowClient.CreateAndRunInstanceAsync(request);

        await _signalManager.WaitAsync("Apple");
        await _signalManager.WaitAsync("Banana");
        await _signalManager.WaitAsync("Cherry");
    }

    private void OnWorkflowInstanceSaved(object? sender, WorkflowInstanceSavedEventArgs e)
    {
        if (e.WorkflowInstance.Status != WorkflowStatus.Finished)
            return;

        if (e.WorkflowInstance.DefinitionId == GreetEmployeesWorkflow.DefinitionId)
            _signalManager.Trigger(GreetEmployeesWorkflowCompletedSignal, e);
    }

    protected override void OnDispose()
    {
        _workflowEvents.WorkflowInstanceSaved -= OnWorkflowInstanceSaved;
    }
}