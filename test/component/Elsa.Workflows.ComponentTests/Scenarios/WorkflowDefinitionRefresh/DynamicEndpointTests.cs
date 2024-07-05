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

        var firstResponse = await client.GetStringAsync("first-value");

        StaticValueHolder.Value = "second-value";
        await _workflowDefinitionsRefresher.RefreshWorkflowDefinitionsAsync(new Runtime.Requests.RefreshWorkflowDefinitionsRequest
        {
            BatchSize = 10,
            DefinitionIds = ["f69f061159adc3ae"]
        }, CancellationToken.None);

        var secondResponse = await client.GetStringAsync("second-value");

        Assert.Equal("", firstResponse);
        Assert.Equal("", secondResponse);
    }
}