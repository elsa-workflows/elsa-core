namespace Elsa.Workflows.ComponentTests.Scenarios.HttpWorkflows;

public class HttpHelloWorldTests(WorkflowServerWebAppFactoryFixture factoryFixture) : ComponentTest(factoryFixture)
{
    [Fact]
    public async Task HelloWorldWorkflow_ShouldRespondWithHelloWorld()
    {
        var client = FactoryFixture.CreateHttpWorkflowClient();
        var response = await client.GetStringAsync("hello-world");
        Assert.Equal("Hello World!", response);
    }
}