using Xunit.Abstractions;

namespace Elsa.Workflows.Api.ComponentTests.Scenarios.HttpWorkflows;

public class HttpHelloWorldTests(ITestOutputHelper testOutputHelper, WorkflowServerTestWebAppFactoryFixture factoryFixture) : ComponentTest(testOutputHelper, factoryFixture)
{
    [Fact]
    public async Task HelloWorldWorkflow_ShouldRespondWithHelloWorld()
    {
        var client = FactoryFixture.CreateHttpWorkflowClient();
        var response = await client.GetStringAsync("hello-world");
        Assert.Equal("Hello World!", response);
    }
}