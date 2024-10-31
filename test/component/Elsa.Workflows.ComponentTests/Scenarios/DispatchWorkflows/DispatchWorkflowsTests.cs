using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Helpers;
using Elsa.Workflows.ComponentTests.Scenarios.DispatchWorkflows.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.DispatchWorkflows;

public class DispatchWorkflowsTests : AppComponentTest
{
    private readonly WorkflowEvents _workflowEvents;
    private readonly SignalManager _signalManager;
    private readonly IWorkflowRuntime _workflowRuntime;

    private readonly object _childWorkflowCompletedSignal = new();
    private readonly object _parentWorkflowCompletedSignal = new();

    public DispatchWorkflowsTests(App app) : base(app)
    {
        _workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        _workflowEvents = Scope.ServiceProvider.GetRequiredService<WorkflowEvents>();
        _signalManager = Scope.ServiceProvider.GetRequiredService<SignalManager>();
        _workflowEvents.WorkflowInstanceSaved += OnWorkflowInstanceSaved;
    }

    [Fact]
    public async Task DispatchAndWaitWorkflow_ShouldWaitForChildWorkflowToComplete()
    {
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(DispatchAndWaitWorkflow.DefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
        var childWorkflowInstanceArgs = await _signalManager.WaitAsync<WorkflowInstanceSavedEventArgs>(_childWorkflowCompletedSignal);
        var parentWorkflowInstanceArgs = await _signalManager.WaitAsync<WorkflowInstanceSavedEventArgs>(_parentWorkflowCompletedSignal);

        Assert.Equal(WorkflowStatus.Finished, childWorkflowInstanceArgs.WorkflowInstance.Status);
        Assert.Equal(WorkflowStatus.Finished, parentWorkflowInstanceArgs.WorkflowInstance.Status);
    }

    private void OnWorkflowInstanceSaved(object? sender, WorkflowInstanceSavedEventArgs e)
    {
        if (e.WorkflowInstance.Status != WorkflowStatus.Finished)
            return;

        if (e.WorkflowInstance.DefinitionId == ChildWorkflow.DefinitionId)
            _signalManager.Trigger(_childWorkflowCompletedSignal, e);

        if (e.WorkflowInstance.DefinitionId == DispatchAndWaitWorkflow.DefinitionId)
            _signalManager.Trigger(_parentWorkflowCompletedSignal, e);
    }

    protected override void OnDispose()
    {
        _workflowEvents.WorkflowInstanceSaved -= OnWorkflowInstanceSaved;
    }
}