using Elsa.Workflows.Api.ComponentTests.Helpers;
using Xunit.Abstractions;

namespace Elsa.Workflows.Api.ComponentTests.Scenarios.HttpWorkflows;

public class HttpHelloWorldTests(ITestOutputHelper testOutputHelper, WorkflowServerTestWebAppFactory factory) : ComponentTest(testOutputHelper, factory)
{
    [Fact]
    public async Task HelloWorldWorkflow_ShouldRespondWithHelloWorld()
    {
        var client = Factory.CreateHttpWorkflowClient();
        var response = await client.GetStringAsync("hello-world");
        Assert.Equal("Hello World!", response);
    }
}