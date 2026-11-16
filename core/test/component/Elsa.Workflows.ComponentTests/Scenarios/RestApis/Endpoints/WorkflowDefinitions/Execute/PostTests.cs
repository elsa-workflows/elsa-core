using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.WorkflowDefinitions.Execute;

public class PostTests(App app) : AppComponentTest(app)
{
    private const string DefinitionId = "3790068018ac4f02";
    private const string Url = "workflow-definitions/{0}/execute";
    private const string NoPublishedVersionMessage = "has no published version";

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
    public async Task Post_MissingDefinitionId_ShouldReturnNotFoundError()
    {
        var client = WorkflowServer.CreateHttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/workflow-definitions//execute");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_NeverPublishedDefinition_ReturnsNotFoundWithMessage()
    {
        var definitionId = await SaveUnpublishedDraftAsync();
        using var response = await PostAsync(definitionId);
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(errors, error => error.StartsWith($"Workflow definition '{definitionId}' {NoPublishedVersionMessage}"));
    }

    [Fact]
    public async Task Post_UnknownDefinition_ReturnsNotFoundWithoutUnpublishedMessage()
    {
        using var response = await PostAsync(Guid.NewGuid().ToString("N"));
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(errors, error => error.Contains(NoPublishedVersionMessage));
    }

    [Fact]
    public async Task Post_NeverPublishedDefinition_WithLatestVersion_ReturnsOk()
    {
        var definitionId = await SaveUnpublishedDraftAsync();
        using var response = await PostAsync(definitionId, new { versionOptions = "Latest" });
        var model = await response.ReadAsJsonAsync<Response>(WorkflowServer.Services);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(WorkflowSubStatus.Finished, model.WorkflowState.SubStatus);
    }

    private async Task<string> SaveUnpublishedDraftAsync()
    {
        var publisher = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionPublisher>();
        var draft = await publisher.NewAsync(new WriteLine("Draft"));
        await publisher.SaveDraftAsync(draft);
        return draft.DefinitionId;
    }

    private Task<HttpResponseMessage> PostAsync(string definitionId, object? body = null)
    {
        var client = WorkflowServer.CreateHttpClient();
        return client.PostAsJsonAsync(string.Format(Url, definitionId), body ?? new { });
    }

    private static async Task<IReadOnlyList<string>> ReadErrorsAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(body))
            return [];

        using var document = JsonDocument.Parse(body);

        if (!document.RootElement.TryGetProperty("errors", out var errors))
            return [];

        return errors.EnumerateObject().SelectMany(x => x.Value.EnumerateArray()).Select(x => x.GetString()!).ToList();
    }
}