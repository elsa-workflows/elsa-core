using System.Net;
using Elsa.Workflows.ComponentTests.Helpers.Services;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowDefinitionRefresh;

public class DynamicEndpointTests : AppComponentTest
{
    private readonly IWorkflowDefinitionsRefresher _workflowDefinitionsRefresher;

    public DynamicEndpointTests(App app) : base(app)
    {
        StaticValueHolder.Value = "first-value";
        _workflowDefinitionsRefresher = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionsRefresher>();
    }

    [Fact]
    public async Task HelloWorldWorkflow_ShouldRespondWithHelloWorld()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();

        var firstResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "first-value"));

        StaticValueHolder.Value = "second-value";
        await _workflowDefinitionsRefresher.RefreshWorkflowDefinitionsAsync(new Runtime.Requests.RefreshWorkflowDefinitionsRequest(), CancellationToken.None);

        var secondResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "second-value"));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }
}