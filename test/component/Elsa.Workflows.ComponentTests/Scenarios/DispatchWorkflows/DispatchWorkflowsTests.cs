using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.ComponentTests.Helpers.Abstractions;
using Elsa.Workflows.ComponentTests.Helpers.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.DispatchWorkflows.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.DispatchWorkflows;

public class DispatchWorkflowsTests : AppComponentTest
{
    private readonly IWorkflowEvents _workflowEvents;
    private readonly ISignalManager _signalManager;
    private readonly IWorkflowRuntime _workflowRuntime;

    private static readonly object ChildWorkflowCompletedSignal = new();
    private static readonly object ParentWorkflowCompletedSignal = new();

    public DispatchWorkflowsTests(App app) : base(app)
    {
        _workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        _workflowEvents = Scope.ServiceProvider.GetRequiredService<IWorkflowEvents>();
        _signalManager = Scope.ServiceProvider.GetRequiredService<ISignalManager>();
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
        var childWorkflowInstanceArgs = await _signalManager.WaitAsync<WorkflowInstanceSavedEventArgs>(ChildWorkflowCompletedSignal);
        var parentWorkflowInstanceArgs = await _signalManager.WaitAsync<WorkflowInstanceSavedEventArgs>(ParentWorkflowCompletedSignal);

        Assert.Equal(WorkflowStatus.Finished, childWorkflowInstanceArgs.WorkflowInstance.Status);
        Assert.Equal(WorkflowStatus.Finished, parentWorkflowInstanceArgs.WorkflowInstance.Status);
    }

    private void OnWorkflowInstanceSaved(object? sender, WorkflowInstanceSavedEventArgs e)
    {
        if (e.WorkflowInstance.Status != WorkflowStatus.Finished)
            return;

        if (e.WorkflowInstance.DefinitionId == ChildWorkflow.DefinitionId)
            _signalManager.Trigger(ChildWorkflowCompletedSignal, e);

        if (e.WorkflowInstance.DefinitionId == DispatchAndWaitWorkflow.DefinitionId)
            _signalManager.Trigger(ParentWorkflowCompletedSignal, e);
    }

    protected override void OnDispose()
    {
        _workflowEvents.WorkflowInstanceSaved -= OnWorkflowInstanceSaved;
    }
}