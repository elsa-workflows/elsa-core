using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.ComponentTests.Scenarios.BulkDispatchWorkflows.Workflows;
using Elsa.Workflows.ComponentTests.Scenarios.DispatchWorkflows.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatchWorkflows;

public class BulkDispatchWorkflowsTests : AppComponentTest
{
    private readonly IWorkflowEvents _workflowEvents;
    private readonly ISignalManager _signalManager;
    private readonly IWorkflowRuntime _workflowRuntime;
    private static readonly object ParentWorkflowCompletedSignal = new();

    public BulkDispatchWorkflowsTests(App app) : base(app)
    {
        _workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        _workflowEvents = Scope.ServiceProvider.GetRequiredService<IWorkflowEvents>();
        _signalManager = Scope.ServiceProvider.GetRequiredService<ISignalManager>();
        _workflowEvents.WorkflowInstanceSaved += OnWorkflowInstanceSaved;
    }

    /// <summary>
    /// Dispatches and waits for child workflows to complete.
    /// </summary>
    [Fact]
    public async Task DispatchAndWaitWorkflow_ShouldWaitForChildWorkflowToComplete()
    {
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(DispatchAndWaitWorkflow.DefinitionId, VersionOptions.Published)
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
        var parentWorkflowInstanceArgs = await _signalManager.WaitAsync<WorkflowInstanceSavedEventArgs>(ParentWorkflowCompletedSignal);
        
        Assert.Equal(WorkflowStatus.Finished, parentWorkflowInstanceArgs.WorkflowInstance.Status);
    }

    private void OnWorkflowInstanceSaved(object? sender, WorkflowInstanceSavedEventArgs e)
    {
        if(e.WorkflowInstance.Status != WorkflowStatus.Finished)
            return;
        
        if(e.WorkflowInstance.DefinitionId == GreetEmployeesWorkflow.DefinitionId)
            _signalManager.Trigger(ParentWorkflowCompletedSignal, e);
    }

    protected override void OnDispose()
    {
        _workflowEvents.WorkflowInstanceSaved -= OnWorkflowInstanceSaved;
    }
}