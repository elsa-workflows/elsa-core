using System.Net;
using System.Text.Json;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.WorkflowDefinitions.Execute;

public class GetTests(App app) : AppComponentTest(app)
{
    private const string DefinitionId = "3790068018ac4f02";
    private const string Url = "workflow-definitions/{0}/execute";
    private const string NoPublishedVersionMessage = "has no published version";

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

    [Fact]
    public async Task Get_NeverPublishedDefinition_ReturnsNotFoundWithMessage()
    {
        var definitionId = await SaveUnpublishedDraftAsync();
        using var response = await GetAsync(definitionId);
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(errors, error => error.StartsWith($"Workflow definition '{definitionId}' {NoPublishedVersionMessage}"));
    }

    [Fact]
    public async Task Get_UnknownDefinition_ReturnsNotFoundWithoutUnpublishedMessage()
    {
        using var response = await GetAsync(Guid.NewGuid().ToString("N"));
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(errors, error => error.Contains(NoPublishedVersionMessage));
    }

    [Fact]
    public async Task Get_NeverPublishedDefinition_WithLatestVersion_ReturnsOk()
    {
        var definitionId = await SaveUnpublishedDraftAsync();
        using var response = await GetAsync(definitionId, "versionOptions=Latest");
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

    private Task<HttpResponseMessage> GetAsync(string definitionId, string? query = null)
    {
        var client = WorkflowServer.CreateHttpClient();
        var url = string.Format(Url, definitionId);

        if (!string.IsNullOrEmpty(query))
            url += "?" + query;

        return client.GetAsync(url);
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
