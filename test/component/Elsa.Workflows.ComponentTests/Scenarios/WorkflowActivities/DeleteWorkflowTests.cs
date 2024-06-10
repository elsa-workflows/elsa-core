using Elsa.Workflows.Contracts;
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
        EnsureWorkflowInRegistry(_scope1, Workflows.DeleteWorkflow.Type);
        
        var workflowDefinitionManager = _scope1.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();
        await workflowDefinitionManager.DeleteByDefinitionIdAsync(Workflows.DeleteWorkflow.DefinitionId);
        
        WorkflowTypeDeletedFromRegistry(_scope1, Workflows.DeleteWorkflow.Type);
    }
    
    [Fact(Skip = "Clustered tests are interfering with other event driven tests")]
    public async Task DeleteWorkflow_Clustered()
    {
        EnsureWorkflowInRegistry(_scope1, Workflows.DeleteWorkflowClustered.Type);
        EnsureWorkflowInRegistry(_scope2, Workflows.DeleteWorkflowClustered.Type);
        EnsureWorkflowInRegistry(_scope3, Workflows.DeleteWorkflowClustered.Type);
        
        var workflowDefinitionManager = _scope1.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();
        await workflowDefinitionManager.DeleteByDefinitionIdAsync(Workflows.DeleteWorkflowClustered.DefinitionId);
        
        WorkflowTypeDeletedFromRegistry(_scope1, Workflows.DeleteWorkflowClustered.Type);
        
        await _signalManager.WaitAsync<WorkflowDefinitionDeletedEventArgs>(WorkflowDeletedSignal);
        WorkflowTypeDeletedFromRegistry(_scope2, Workflows.DeleteWorkflowClustered.Type);
        WorkflowTypeDeletedFromRegistry(_scope3, Workflows.DeleteWorkflowClustered.Type);
    }

    private static void EnsureWorkflowInRegistry(IServiceScope scope, string type)
    {
        var activityRegistry = scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var descriptor = activityRegistry.Find(type);
        Assert.NotNull(descriptor);
    }

    private static void WorkflowTypeDeletedFromRegistry(IServiceScope scope, string type)
    {
        var activityRegistry = scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var descriptor = activityRegistry.Find(type);

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