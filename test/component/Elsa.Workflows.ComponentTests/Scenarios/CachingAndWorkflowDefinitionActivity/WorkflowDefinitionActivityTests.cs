using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.ComponentTests.Scenarios.CachingAndWorkflowDefinitionActivity;

// Tests the behavior of the WorkflowDefinitionActivity.
// See https://github.com/elsa-workflows/elsa-core/issues/5314
public class WorkflowDefinitionActivityTests : AppComponentTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private const string GrandChildDefinitionId = "29595e7b37a4836d";

    private readonly IWorkflowDefinitionCacheManager _workflowDefinitionCacheManager;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly HttpClient _httpWorkflowClient;

    public WorkflowDefinitionActivityTests(App app, ITestOutputHelper testOutputHelper) : base(app)
    {
        _testOutputHelper = testOutputHelper;
        _httpWorkflowClient = WorkflowServer.CreateHttpWorkflowClient();
        _workflowDefinitionCacheManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionCacheManager>();
        _workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
    }

    [Fact]
    public async Task SendHttpRequest_WhileEvictingCache_ShouldNotGenerateFaults()
    {
        var requestTasks = Enumerable.Range(0, 200).Select(SendRequestAsync).ToList();
        await Task.WhenAll(requestTasks);

        var filter = new WorkflowInstanceFilter
        {
            WorkflowSubStatus = WorkflowSubStatus.Faulted,
            DefinitionIds = ["189be5173f90b1f6", "a3390d1f4c2594a8", "29595e7b37a4836d"]
        };
        var faultedWorkflows = (await _workflowInstanceStore.FindManyAsync(filter)).ToList();
        var faultCount = faultedWorkflows.Count;

        foreach (var faultedWorkflow in faultedWorkflows)
        foreach (var incident in faultedWorkflow.WorkflowState.Incidents)
            _testOutputHelper.WriteLine(incident.Message);

        Assert.Equal(0, faultCount);
    }

    private async Task SendRequestAsync(int index = 0)
    {
        var requestTask = _httpWorkflowClient.PostAsync("parent", new StringContent("{}"));
        var evictionTask = _workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(GrandChildDefinitionId);
        await Task.WhenAll(requestTask, evictionTask);
    }
}