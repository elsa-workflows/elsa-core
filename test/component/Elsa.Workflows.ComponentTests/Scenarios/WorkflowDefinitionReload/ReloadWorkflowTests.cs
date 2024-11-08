using System.Net;
using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Helpers.Materializers;
using Elsa.Workflows.ComponentTests.Helpers.WorkflowProviders;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowDefinitionReload;

public class ReloadWorkflowTests : AppComponentTest
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
    private readonly IWorkflowDefinitionsReloader _workflowDefinitionsReloader;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly TestWorkflowProvider _testWorkflowProvider;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IActivityRegistry _activityRegistry;

    public ReloadWorkflowTests(App app) : base(app)
    {
        _workflowDefinitionManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();
        _workflowDefinitionsReloader = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionsReloader>();
        _workflowBuilderFactory = Scope.ServiceProvider.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowDefinitionService = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        _activityRegistry = Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        var workflowProviders = Scope.ServiceProvider.GetRequiredService<IEnumerable<IWorkflowProvider>>();
        _testWorkflowProvider = (TestWorkflowProvider)workflowProviders.First(x => x is TestWorkflowProvider);
    }

    [Fact]
    public async Task Reloading_AfterRemovingTheWorkflow_ShouldMakeWorkflowReachableAgain()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        await _workflowDefinitionManager.DeleteByDefinitionIdAsync("f68b09bc-2013-4617-b82f-d76b6819a624", CancellationToken.None);
        var firstResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "reload-test"));
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();
        var secondResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "reload-test"));
        Assert.Equal(HttpStatusCode.NotFound, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Reloading_AfterUpdatingSourceProvider_ShouldRefreshCaches()
    {
        var definitionId = Guid.NewGuid().ToString();
        var definitionVersionId1 = Guid.NewGuid().ToString();
        var workflowV1 = await BuildWorkflowAsync(definitionId, definitionVersionId1, 1);
        
        // Set up the initial workflow version.
        _testWorkflowProvider.MaterializedWorkflows = [workflowV1];
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();
        var definitionV1 = await _workflowDefinitionService.FindWorkflowGraphAsync(definitionId, VersionOptions.Latest);
        Assert.Equal(definitionVersionId1, definitionV1!.Workflow.Identity.Id);
        
        // Simulate the workflow provider to have a new version available.
        var definitionVersionId2 = Guid.NewGuid().ToString();
        var workflowV2 = await BuildWorkflowAsync(definitionId, definitionVersionId2, 2);
        _testWorkflowProvider.MaterializedWorkflows = [workflowV1, workflowV2];
        
        // Reload the workflow definitions.
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();
        
        // Assert that the workflow definition service finds the updated workflow version.
        var definitionV2 = await _workflowDefinitionService.FindWorkflowGraphAsync(definitionId, VersionOptions.Latest);
        Assert.Equal(definitionVersionId2, definitionV2!.Workflow.Identity.Id);
    }
    
    [Fact]
    public async Task Reloading_AfterUpdatingSourceProvider_ShouldRefreshActivityRegistry()
    {
        var definitionId = Guid.NewGuid().ToString();
        var definitionVersionId1 = Guid.NewGuid().ToString();
        var workflowV1 = await BuildWorkflowAsync(definitionId, definitionVersionId1, 1);
        
        // Set up the initial workflow version.
        _testWorkflowProvider.MaterializedWorkflows = [workflowV1];
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();
        var activityTypeName = workflowV1.Workflow.Name.Pascalize();
        var activityV1 =  _activityRegistry.Find(activityTypeName);
        Assert.Equal(1, activityV1!.Version);
        
        // Simulate the workflow provider to have a new version available.
        var definitionVersionId2 = Guid.NewGuid().ToString();
        var workflowV2 = await BuildWorkflowAsync(definitionId, definitionVersionId2, 2);
        _testWorkflowProvider.MaterializedWorkflows = [workflowV1, workflowV2];
        
        // Reload the workflow definitions.
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();
        
        // Assert that the activity registry contains a new activity descriptor representing the new workflow version.
        var activityV2 = _activityRegistry.Find(activityTypeName)!;
        Assert.Equal(2, activityV2.Version);
        
        // Cleanup: Delete the workflow definition and its versions.
        await _workflowDefinitionManager.DeleteByDefinitionIdAsync(definitionId, CancellationToken.None);
    }
    
    private async Task<MaterializedWorkflow> BuildWorkflowAsync(string definitionId, string definitionVersionId, int version)
    {
        var builder = _workflowBuilderFactory.CreateBuilder();
        builder.DefinitionId = definitionId;
        builder.Id = definitionVersionId;
        builder.Version = version;
        builder.Name = definitionId;
        builder.Root = new WriteLine($"Version {version}");
        builder.WorkflowOptions.UsableAsActivity = true;
        var workflow = await builder.BuildWorkflowAsync();
        workflow.Name = definitionId;
        return new MaterializedWorkflow(workflow, _testWorkflowProvider.Name, TestWorkflowMaterializer.MaterializerName);
    }
}