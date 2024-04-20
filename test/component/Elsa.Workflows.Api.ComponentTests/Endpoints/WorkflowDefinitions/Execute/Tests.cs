using System.Net;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;

namespace Elsa.Workflows.Api.ComponentTests.Endpoints.WorkflowDefinitions.Execute;

public class Tests(WorkflowServerTestWebAppFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task Post_ShouldReturnOk_WhenWorkflowDefinitionIsValid()
    {
        var client = Factory.CreateApiClient<IExecuteWorkflowApi>();
        using var response = await client.ExecuteAsync("hello-world");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}