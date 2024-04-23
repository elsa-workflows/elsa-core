namespace Elsa.Workflows.ComponentTests.Scenarios.HttpWorkflows;

public class HttpHelloWorldTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task HelloWorldWorkflow_ShouldRespondWithHelloWorld()
    {
        var client = WorkflowServer.CreateHttpWorkflowClient();
        var response = await client.GetStringAsync("hello-world");
        Assert.Equal("Hello World!", response);
    }
}