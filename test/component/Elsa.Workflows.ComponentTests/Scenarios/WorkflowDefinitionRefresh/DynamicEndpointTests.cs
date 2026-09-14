using System.Net;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Services;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.WorkflowDefinitionRefresh;

public class DynamicEndpointTests : AppComponentTest
{
    private TestJavaScriptState _javaScriptState = null!;
    private IWorkflowDefinitionsRefresher _workflowDefinitionsRefresher = null!;

    public DynamicEndpointTests(App app) : base(app)
    {
    }

    protected override ValueTask OnInitializeAsync()
    {
        _javaScriptState = Scope.ServiceProvider.GetRequiredService<TestJavaScriptState>();
        _javaScriptState.Value = "first-value";
        _workflowDefinitionsRefresher = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionsRefresher>();
        return ValueTask.CompletedTask;
    }

    [Test]
    public async Task ChangingEndpointValueThenRefresh_WorkflowShouldRespondToTheNewValue()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();

        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, "first-value");
        using var firstResponse = await client.SendAsync(firstRequest);

        _javaScriptState.Value = "second-value";
        _ = await _workflowDefinitionsRefresher.RefreshWorkflowDefinitionsAsync(
            new() { DefinitionIds = ["f69f061159adc3ae"] }, CancellationToken.None);

        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, "second-value");
        using var secondResponse = await client.SendAsync(secondRequest);

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
