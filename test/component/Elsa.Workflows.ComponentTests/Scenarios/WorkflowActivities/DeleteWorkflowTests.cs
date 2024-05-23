using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowActivities;

public class DeleteWorkflowTests : AppComponentTest
{
    private readonly ISignalManager _signalManager;
    private readonly IWorkflowDefinitionEvents _workflowDefinitionEvents;
    private readonly IServiceScope _scope1;
    private readonly IServiceScope _scope2;
    private readonly IServiceScope _scope3;
    private static readonly object WorkflowDeletedSignal = new();
    
    public DeleteWorkflowTests(App app) : base(app)
    {
        _scope1 = app.Cluster.Pod1.Services.CreateScope();
        // Disabled these scope creations since this prevents events from firing in other tests.
        // _scope2 = app.Cluster.Pod2.Services.CreateScope();
        // _scope3 = app.Cluster.Pod3.Services.CreateScope();
        _signalManager = Scope.ServiceProvider.GetRequiredService<ISignalManager>();
        _workflowDefinitionEvents = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionEvents>();
        _workflowDefinitionEvents.WorkflowDefinitionDeleted += OnWorkflowDefinionDeleted;
    }

    [Fact]
    public async Task DeleteWorkflow()
    {
        EnsureWorkflowInRegistry(_scope1);
        
        var workflowDefinitionManager = _scope1.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();
        await workflowDefinitionManager.DeleteByDefinitionIdAsync(Workflows.DeleteWorkflow.DefinitionId);
        
        WorkflowTypeDeletedFromRegistry(_scope1);
    }
    
    [Fact(Skip = "Clustered tests are interfering with other event driven tests")]
    public async Task DeleteWorkflow_Clustered()
    {
        EnsureWorkflowInRegistry(_scope1);
        EnsureWorkflowInRegistry(_scope2);
        EnsureWorkflowInRegistry(_scope3);
        
        var workflowDefinitionManager = _scope1.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();
        await workflowDefinitionManager.DeleteByDefinitionIdAsync(Workflows.DeleteWorkflow.DefinitionId);
        
        WorkflowTypeDeletedFromRegistry(_scope1);
        
        await _signalManager.WaitAsync<WorkflowDefinitionDeletedEventArgs>(WorkflowDeletedSignal);
        WorkflowTypeDeletedFromRegistry(_scope2);
        WorkflowTypeDeletedFromRegistry(_scope3);
    }

    private void EnsureWorkflowInRegistry(IServiceScope scope)
    {
        var activityRegistry = scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var descriptor = activityRegistry.Find(Workflows.DeleteWorkflow.Type);
        if (descriptor is null)
            activityRegistry.Add(typeof(WorkflowDefinitionActivityProvider), descriptor);
    }

    private void WorkflowTypeDeletedFromRegistry(IServiceScope scope)
    {
        var activityRegistry = scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var descriptor = activityRegistry.Find(Workflows.DeleteWorkflow.Type);

        Assert.Null(descriptor);
    }

    private void OnWorkflowDefinionDeleted(object? sender, WorkflowDefinitionDeletedEventArgs args)
    {
        if (args.DefinitionId == Workflows.DeleteWorkflow.DefinitionId)
        {
            _signalManager.Trigger(WorkflowDeletedSignal, args);
        }
    }

    protected override void OnDispose()
    {
        _workflowDefinitionEvents.WorkflowDefinitionDeleted -= OnWorkflowDefinionDeleted;
    }
}