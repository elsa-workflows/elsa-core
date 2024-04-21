using System.Net;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Xunit.Abstractions;

namespace Elsa.Workflows.Api.ComponentTests.Scenarios.BasicWorkflows;

public class HelloWorldTests(ITestOutputHelper testOutputHelper, WorkflowServerWebAppFactoryFixture factoryFixture) : ComponentTest(testOutputHelper, factoryFixture)
{
    [Fact]
    public async Task HelloWorldWorkflow_ShouldReturnOk()
    {
        var client = FactoryFixture.CreateApiClient<IExecuteWorkflowApi>();
        using var response = await client.ExecuteAsync("hello-world");
        var model = await response.ReadAsJsonAsync<Response>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkflowSubStatus.Finished, model.WorkflowState.SubStatus);
    }
}