using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Testing.Shared.Authorization;
using Elsa.UnitTests.Shared;
using Elsa.Workflows.Api.Features;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.WorkflowDefinitions;

[Collection(nameof(FastEndpointsCollection))]
public class ExecuteEndpointTests : IAsyncLifetime
{
    private const string Path = "/workflow-definitions/{0}/execute";
    private const string ExecutePermission = "workflows/definitions:execute";
    private const string NoPublishedVersionMessage = "has no published version";
    private readonly MemoryWorkflowDefinitionStore _store = new(new MemoryStore<WorkflowDefinition>());
    private AuthorizationTestHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = await AuthorizationTestHost.StartAsync<WorkflowsApiFeature>(
            type => type.Namespace == "Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute",
            services =>
            {
                services.AddElsa(elsa => elsa.UseWorkflowsApi());
                services.AddSingleton<IWorkflowDefinitionStore>(_store);
            });
    }

    public async Task DisposeAsync()
    {
        FastEndpointsResolver.Reset();
        await _host.DisposeAsync();
    }

    [Fact]
    public async Task Get_NeverPublishedDefinition_ReturnsNotFoundWithMessage()
    {
        var definitionId = await SaveUnpublishedDraftAsync();
        using var response = await ExecuteAsync(HttpMethod.Get, definitionId);
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(errors, error => error.StartsWith($"Workflow definition '{definitionId}' {NoPublishedVersionMessage}"));
    }

    [Fact]
    public async Task Post_NeverPublishedDefinition_ReturnsNotFoundWithMessage()
    {
        var definitionId = await SaveUnpublishedDraftAsync();
        using var response = await ExecuteAsync(HttpMethod.Post, definitionId, JsonContent.Create(new { }));
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(errors, error => error.StartsWith($"Workflow definition '{definitionId}' {NoPublishedVersionMessage}"));
    }

    [Fact]
    public async Task Get_UnknownDefinition_ReturnsNotFoundWithoutUnpublishedMessage()
    {
        using var response = await ExecuteAsync(HttpMethod.Get, Guid.NewGuid().ToString("N"));
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(errors, error => error.Contains(NoPublishedVersionMessage));
    }

    [Fact]
    public async Task Post_UnknownDefinition_ReturnsNotFoundWithoutUnpublishedMessage()
    {
        using var response = await ExecuteAsync(HttpMethod.Post, Guid.NewGuid().ToString("N"), JsonContent.Create(new { }));
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(errors, error => error.Contains(NoPublishedVersionMessage));
    }

    [Fact]
    public async Task Get_PublishedDefinitionWithMissingMaterializer_ReturnsNotFoundWithoutUnpublishedMessage()
    {
        var definitionId = await SavePublishedDefinitionAsync("missing-materializer");
        using var response = await ExecuteAsync(HttpMethod.Get, definitionId);
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(errors, error => error.Contains(NoPublishedVersionMessage));
    }

    [Fact]
    public async Task Post_PublishedDefinitionWithMissingMaterializer_ReturnsNotFoundWithoutUnpublishedMessage()
    {
        var definitionId = await SavePublishedDefinitionAsync("missing-materializer");
        using var response = await ExecuteAsync(HttpMethod.Post, definitionId, JsonContent.Create(new { }));
        var errors = await ReadErrorsAsync(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(errors, error => error.Contains(NoPublishedVersionMessage));
    }

    private async Task<string> SaveUnpublishedDraftAsync()
    {
        var definitionId = Guid.NewGuid().ToString("N");
        await _store.SaveAsync(new WorkflowDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            DefinitionId = definitionId,
            Version = 1,
            IsLatest = true,
            IsPublished = false,
            Name = "Draft",
            MaterializerName = JsonWorkflowMaterializer.MaterializerName,
            StringData = "{}"
        });
        return definitionId;
    }

    private async Task<string> SavePublishedDefinitionAsync(string materializerName)
    {
        var definitionId = Guid.NewGuid().ToString("N");
        await _store.SaveAsync(new WorkflowDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            DefinitionId = definitionId,
            Version = 1,
            IsLatest = true,
            IsPublished = true,
            Name = "Published",
            MaterializerName = materializerName,
            StringData = "{}"
        });
        return definitionId;
    }

    private Task<HttpResponseMessage> ExecuteAsync(HttpMethod method, string definitionId, HttpContent? content = null) =>
        _host.SendAsync(method, string.Format(Path, definitionId), ExecutePermission, content: content);

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
