using System.Net;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.WorkflowDefinitions.Execute;

public class GetTests(App app) : AppComponentTest(app)
{
    private const string DefinitionId = "3790068018ac4f02";
    private const string Url = "workflow-definitions/{0}/execute";

    [Fact]
    public async Task Get_WithCorrelationId_ShouldReturnOk()
    {
        var client = WorkflowServer.CreateHttpClient();
        var url = string.Format(Url, DefinitionId) + "?correlationId=" + Guid.NewGuid();
        using var response = await client.GetAsync(url);
        var model = await response.ReadAsJsonAsync<Response>(WorkflowServer.Services);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkflowSubStatus.Finished, model.WorkflowState.SubStatus);
    }

    [Fact]
    public async Task Get_WithoutCorrelationId_ShouldReturnOk()
    {
        var client = WorkflowServer.CreateHttpClient();
        var url = string.Format(Url, DefinitionId);
        using var response = await client.GetAsync(url);
        var model = await response.ReadAsJsonAsync<Response>(WorkflowServer.Services);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkflowSubStatus.Finished, model.WorkflowState.SubStatus);
    }

    [Fact]
    public async Task Get_MissingDefinitionId_ShouldReturnNotFoundError()
    {
        var client = WorkflowServer.CreateHttpClient();
        var url = "/workflow-definitions//execute";
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
