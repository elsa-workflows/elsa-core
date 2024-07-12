using System.Net;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowDefinitionReload;

public class RemoveReloadWorkflowTests : AppComponentTest
{
    private readonly IWorkflowDefinitionManager _workflowDefinitionManager;
    private readonly IWorkflowDefinitionsReloader _workflowDefinitionsReloader;

    public RemoveReloadWorkflowTests(App app) : base(app)
    {
        _workflowDefinitionManager = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionManager>();
        _workflowDefinitionsReloader = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionsReloader>();
    }

    [Fact]
    public async Task RemovingTheWorkflowThenReload_WorkflowShouldBeReachableAgain()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();

        var result = await _workflowDefinitionManager.DeleteByDefinitionIdAsync("f68b09bc-2013-4617-b82f-d76b6819a624", CancellationToken.None);

        var firstResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "reload-test"));

        await _workflowDefinitionsReloader.ReloadWorkflowDefinitionsAsync(CancellationToken.None);

        var secondResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "reload-test"));

        Assert.Equal(HttpStatusCode.NotFound, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }
}