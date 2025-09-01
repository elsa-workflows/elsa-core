using System.Net;
using System.Text;
using System.Text.Json;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.WorkflowDefinitions.Execute;

public class PostTests(App app) : AppComponentTest(app)
{
    private const string DefinitionId = "3790068018ac4f02";
    private const string Url = "workflow-definitions/{0}/execute";

    [Fact]
    public async Task Post_WithValidJsonBody_ShouldReturnOk()
    {
        var client = WorkflowServer.CreateHttpClient();
        var requestBody = JsonSerializer.Serialize(new PostRequest
        {
            CorrelationId = Guid.NewGuid().ToString()
        });
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(string.Format(Url, DefinitionId), content);
        var model = await response.ReadAsJsonAsync<Response>(WorkflowServer.Services);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkflowSubStatus.Finished, model.WorkflowState.SubStatus);
    }

    [Fact]
    public async Task Post_WithoutBodyAndWithoutContentType_ShouldReturnOk()
    {
        var client = WorkflowServer.CreateHttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, string.Format(Url, DefinitionId));
        // No content, no content-type
        using var response = await client.SendAsync(request);
        var model = await response.ReadAsJsonAsync<Response>(WorkflowServer.Services);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkflowSubStatus.Finished, model.WorkflowState.SubStatus);
    }

    [Fact]
    public async Task Post_WithoutBodyButWithContentType_ShouldReturnOk()
    {
        var client = WorkflowServer.CreateHttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, string.Format(Url, DefinitionId));
        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        var model = await response.ReadAsJsonAsync<Response>(WorkflowServer.Services);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkflowSubStatus.Finished, model.WorkflowState.SubStatus);
    }

    [Fact]
    public async Task Post_MissingDefinitionId_ShouldReturnValidationError()
    {
        var client = WorkflowServer.CreateHttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/workflow-definitions//execute");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}