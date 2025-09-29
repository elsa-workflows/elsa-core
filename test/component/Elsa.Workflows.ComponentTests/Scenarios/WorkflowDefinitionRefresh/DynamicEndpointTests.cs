using System.Net;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Runtime;
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
    public async Task ChangingEndpointValueThenRefresh_WorkflowShouldRespondToTheNewValue()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();

        var firstResponse = await client.SendAsync(new(HttpMethod.Get, "first-value"));

        StaticValueHolder.Value = "second-value";
        _ = await _workflowDefinitionsRefresher.RefreshWorkflowDefinitionsAsync(
            new() { DefinitionIds = ["f69f061159adc3ae"] }, CancellationToken.None);

        var secondResponse = await client.SendAsync(new(HttpMethod.Get, "second-value"));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
    }
}