using System.Net;
using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Materializers;
using Elsa.Workflows.ComponentTests.WorkflowProviders;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowDefinitionReload;

public class ReloadWorkflowTests : AppComponentTest
{
    private readonly IActivityRegistry _activityRegistry;
    private readonly TestWorkflowProvider _testWorkflowProvider;
    private readonly IWorkflowBuilderFactory _workflowBuilderFactory;
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowDefinitionsReloader _workflowDefinitionsReloader;
    private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
    private readonly ITriggerStore _triggerStore;

    public ReloadWorkflowTests(App app) : base(app)
    {
        _workflowDefinitionManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();
        _workflowDefinitionsReloader = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionsReloader>();
        _workflowBuilderFactory = Scope.ServiceProvider.GetRequiredService<IWorkflowBuilderFactory>();
        _workflowDefinitionService = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();
        _workflowDefinitionPublisher = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();
        _activityRegistry = Scope.ServiceProvider.GetRequiredService<IActivityRegistry>();
        _triggerStore = Scope.ServiceProvider.GetRequiredService<ITriggerStore>();
        var workflowsProviders = Scope.ServiceProvider.GetRequiredService<IEnumerable<IWorkflowsProvider>>();
        _testWorkflowProvider = (TestWorkflowProvider)workflowsProviders.First(x => x is TestWorkflowProvider);
    }

    [Fact]
    public async Task Reloading_AfterRemovingTheWorkflow_ShouldMakeWorkflowReachableAgain()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        await _workflowDefinitionManager.DeleteByDefinitionIdAsync("f68b09bc-2013-4617-b82f-d76b6819a624", CancellationToken.None);
        var firstResponse = await client.SendAsync(new(HttpMethod.Get, "reload-test"));
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();
        var secondResponse = await client.SendAsync(new(HttpMethod.Get, "reload-test"));
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
        
        // Cleanup: Delete the workflow definition and its versions.
        await _workflowDefinitionManager.DeleteByDefinitionIdAsync(definitionId, CancellationToken.None);
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
        var activityV1 = _activityRegistry.Find(activityTypeName);
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

    [Fact]
    public async Task Reloading_AfterPublishingNewVersion_ShouldPersistTriggers()
    {
        // Get the initial workflow definition.
        const string definitionId = "f68b09bc-2013-4617-b82f-d76b6819a624";
        var initialDefinition = await _workflowDefinitionService.FindWorkflowDefinitionAsync(definitionId, VersionOptions.Published, CancellationToken.None);
        Assert.NotNull(initialDefinition);

        // Assert that triggers exist initially.
        var initialTrigger = await _triggerStore.FindAsync(new(){ WorkflowDefinitionId = definitionId}, CancellationToken.None);
        Assert.NotNull(initialTrigger);

        // Publish a new version of the workflow.
        var draftDefinition = await _workflowDefinitionPublisher.GetDraftAsync(definitionId, VersionOptions.Latest);
        Assert.NotNull(draftDefinition);
        await _workflowDefinitionPublisher.PublishAsync(draftDefinition, CancellationToken.None);
        
        // Assert we are at version 2.
        var v2Definition = await _workflowDefinitionService.FindWorkflowDefinitionAsync(definitionId, VersionOptions.Published, CancellationToken.None);
        Assert.NotNull(v2Definition);
        Assert.Equal(2, v2Definition.Version);

        // Reload the workflow definitions.
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();

        // Assert that triggers still exist after reload.
        var reloadedTrigger = await _triggerStore.FindAsync(new(){ WorkflowDefinitionId = definitionId}, CancellationToken.None);
        Assert.NotNull(reloadedTrigger);
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
        return new(workflow, _testWorkflowProvider.Name, TestWorkflowMaterializer.MaterializerName);
    }
}