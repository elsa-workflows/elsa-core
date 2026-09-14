using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.HttpWorkflows;

public class HttpHelloWorldTests(App app) : AppComponentTest(app)
{
    [Test]
    public async Task HelloWorldWorkflow_ShouldRespondWithHelloWorld()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var response = await client.GetStringAsync("hello-world");
        await Assert.That(response).IsEqualTo("Hello World!");
    }
}