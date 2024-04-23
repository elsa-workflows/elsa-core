using Elsa.Workflows.ComponentTests.Scenarios.DispatchWorkflows.Workflows;
using Elsa.Workflows.Runtime.Contracts;
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
        await _workflowRuntime.StartWorkflowAsync(DispatchAndWaitWorkflow.DefinitionId);
        var childWorkflowInstanceArgs = await _signalManager.WaitAsync<WorkflowInstanceSavedEventArgs>(ChildWorkflowCompletedSignal, 50000);
        var parentWorkflowInstanceArgs = await _signalManager.WaitAsync<WorkflowInstanceSavedEventArgs>(ParentWorkflowCompletedSignal, 50000);
        
        Assert.Equal(WorkflowStatus.Finished, childWorkflowInstanceArgs.WorkflowInstance.Status);
        Assert.Equal(WorkflowStatus.Finished, parentWorkflowInstanceArgs.WorkflowInstance.Status);
    }

    private void OnWorkflowInstanceSaved(object? sender, WorkflowInstanceSavedEventArgs e)
    {
        if(e.WorkflowInstance.Status != WorkflowStatus.Finished)
            return;
        
        if(e.WorkflowInstance.DefinitionId == ChildWorkflow.DefinitionId)
            _signalManager.Trigger(ChildWorkflowCompletedSignal, e);
        
        if(e.WorkflowInstance.DefinitionId == DispatchAndWaitWorkflow.DefinitionId)
            _signalManager.Trigger(ParentWorkflowCompletedSignal, e);
    }

    protected override void OnDispose()
    {
        _workflowEvents.WorkflowInstanceSaved -= OnWorkflowInstanceSaved;
    }
}