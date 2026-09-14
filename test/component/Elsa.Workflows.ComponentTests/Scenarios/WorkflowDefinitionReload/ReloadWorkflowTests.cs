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
    private IActivityRegistry _activityRegistry = null!;
    private TestWorkflowProvider _testWorkflowProvider = null!;
    private IWorkflowBuilderFactory _workflowBuilderFactory = null!;
    private IWorkflowDefinitionManager _workflowDefinitionManager = null!;
    private IWorkflowDefinitionService _workflowDefinitionService = null!;
    private IWorkflowDefinitionsReloader _workflowDefinitionsReloader = null!;
    private IWorkflowDefinitionPublisher _workflowDefinitionPublisher = null!;
    private ITriggerStore _triggerStore = null!;

    public ReloadWorkflowTests(App app) : base(app)
    {
    }

    protected override ValueTask OnInitializeAsync()
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
        return ValueTask.CompletedTask;
    }

    [Test]
    public async Task Reloading_AfterRemovingTheWorkflow_ShouldMakeWorkflowReachableAgain()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        await _workflowDefinitionManager.DeleteByDefinitionIdAsync("f68b09bc-2013-4617-b82f-d76b6819a624", CancellationToken.None);
        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, "reload-test");
        using var firstResponse = await client.SendAsync(firstRequest);
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();
        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, "reload-test");
        using var secondResponse = await client.SendAsync(secondRequest);
        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Reloading_AfterUpdatingSourceProvider_ShouldRefreshCaches()
    {
        var definitionId = Guid.NewGuid().ToString();
        var definitionVersionId1 = Guid.NewGuid().ToString();
        var workflowV1 = await BuildWorkflowAsync(definitionId, definitionVersionId1, 1);

        // Set up the initial workflow version.
        _testWorkflowProvider.MaterializedWorkflows = [workflowV1];
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();
        var definitionV1 = await _workflowDefinitionService.FindWorkflowGraphAsync(definitionId, VersionOptions.Latest);
        await Assert.That(definitionV1!.Workflow.Identity.Id).IsEqualTo(definitionVersionId1);

        // Simulate the workflow provider to have a new version available.
        var definitionVersionId2 = Guid.NewGuid().ToString();
        var workflowV2 = await BuildWorkflowAsync(definitionId, definitionVersionId2, 2);
        _testWorkflowProvider.MaterializedWorkflows = [workflowV1, workflowV2];

        // Reload the workflow definitions.
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();

        // Assert that the workflow definition service finds the updated workflow version.
        var definitionV2 = await _workflowDefinitionService.FindWorkflowGraphAsync(definitionId, VersionOptions.Latest);
        await Assert.That(definitionV2!.Workflow.Identity.Id).IsEqualTo(definitionVersionId2);

        // Cleanup: Delete the workflow definition and its versions.
        await _workflowDefinitionManager.DeleteByDefinitionIdAsync(definitionId, CancellationToken.None);
    }

    [Test]
    public async Task Reloading_AfterUpdatingSourceProvider_ShouldRefreshActivityRegistry()
    {
        var definitionId = Guid.NewGuid().ToString();
        var definitionVersionId1 = Guid.NewGuid().ToString();
        var workflowV1 = await BuildWorkflowAsync(definitionId, definitionVersionId1, 1);

        // Set up the initial workflow version.
        _testWorkflowProvider.MaterializedWorkflows = [workflowV1];
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();
        var activityTypeName = workflowV1.Workflow.Name!.Pascalize();
        var activityV1 = _activityRegistry.Find(activityTypeName);
        await Assert.That(activityV1!.Version).IsEqualTo(1);

        // Simulate the workflow provider to have a new version available.
        var definitionVersionId2 = Guid.NewGuid().ToString();
        var workflowV2 = await BuildWorkflowAsync(definitionId, definitionVersionId2, 2);
        _testWorkflowProvider.MaterializedWorkflows = [workflowV1, workflowV2];

        // Reload the workflow definitions.
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();

        // Assert that the activity registry contains a new activity descriptor representing the new workflow version.
        var activityV2 = _activityRegistry.Find(activityTypeName)!;
        await Assert.That(activityV2.Version).IsEqualTo(2);

        // Cleanup: Delete the workflow definition and its versions.
        await _workflowDefinitionManager.DeleteByDefinitionIdAsync(definitionId, CancellationToken.None);
    }

    [Test]
    public async Task Reloading_AfterPublishingNewVersion_ShouldPersistTriggers()
    {
        // Get the initial workflow definition.
        const string definitionId = "f68b09bc-2013-4617-b82f-d76b6819a624";
        var initialDefinition = await _workflowDefinitionService.FindWorkflowDefinitionAsync(definitionId, VersionOptions.Published, CancellationToken.None);
        await Assert.That(initialDefinition).IsNotNull();

        // Assert that triggers exist initially.
        var initialTrigger = await _triggerStore.FindAsync(new(){ WorkflowDefinitionId = definitionId}, CancellationToken.None);
        await Assert.That(initialTrigger).IsNotNull();

        // Publish a new version of the workflow.
        var draftDefinition = await _workflowDefinitionPublisher.GetDraftAsync(definitionId, VersionOptions.Latest);
        await Assert.That(draftDefinition).IsNotNull();
        await _workflowDefinitionPublisher.PublishAsync(draftDefinition, CancellationToken.None);
        
        // Assert we are at version 2.
        var v2Definition = await _workflowDefinitionService.FindWorkflowDefinitionAsync(definitionId, VersionOptions.Published, CancellationToken.None);
        await Assert.That(v2Definition).IsNotNull();
        await Assert.That(v2Definition.Version).IsEqualTo(2);

        // Reload the workflow definitions.
        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync();

        // Assert that triggers still exist after reload.
        var reloadedTrigger = await _triggerStore.FindAsync(new(){ WorkflowDefinitionId = definitionId}, CancellationToken.None);
        await Assert.That(reloadedTrigger).IsNotNull();
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
