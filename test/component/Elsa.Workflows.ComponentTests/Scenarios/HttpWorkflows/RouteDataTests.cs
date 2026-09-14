using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.HttpWorkflows;

public class RouteDataTests(App app) : AppComponentTest(app)
{
    [Test]
    public async Task RouteDataWorkflow_ShouldRespondWithRouteData()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var response = await client.GetStringAsync("orders/42");
        await Assert.That(response).IsEqualTo("42");
    }
}