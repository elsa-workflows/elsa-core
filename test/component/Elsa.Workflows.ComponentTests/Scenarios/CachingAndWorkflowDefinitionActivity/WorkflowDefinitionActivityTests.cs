using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.CachingAndWorkflowDefinitionActivity;

// Tests the behavior of the WorkflowDefinitionActivity.
// See https://github.com/elsa-workflows/elsa-core/issues/5314
public class WorkflowDefinitionActivityTests : AppComponentTest
{

    private const string GrandChildDefinitionId = "29595e7b37a4836d";

    private IWorkflowDefinitionCacheManager _workflowDefinitionCacheManager = null!;
    private IWorkflowInstanceStore _workflowInstanceStore = null!;
    private HttpClient _httpWorkflowClient = null!;

    public WorkflowDefinitionActivityTests(App app) : base(app)
    {
    }

    protected override ValueTask OnInitializeAsync()
    {
        _httpWorkflowClient = WorkflowServer.CreateHttpWorkflowClient();
        _workflowDefinitionCacheManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionCacheManager>();
        _workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        return ValueTask.CompletedTask;
    }

    [Test]
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
            TestContext.Current!.Output.StandardOutput.WriteLine(incident.Message);

        await Assert.That(faultCount).IsEqualTo(0);
    }

    private async Task SendRequestAsync(int index = 0)
    {
        using var content = new StringContent("{}");
        var requestTask = _httpWorkflowClient.PostAsync("parent", content);
        var evictionTask = _workflowDefinitionCacheManager.EvictWorkflowDefinitionAsync(GrandChildDefinitionId);
        try
        {
            await Task.WhenAll(requestTask, evictionTask);
        }
        finally
        {
            if (requestTask.IsCompletedSuccessfully)
                requestTask.Result.Dispose();
        }
    }
}
