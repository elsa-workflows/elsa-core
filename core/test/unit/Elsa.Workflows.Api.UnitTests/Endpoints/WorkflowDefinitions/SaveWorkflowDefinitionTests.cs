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
using Elsa.Workflows.Management.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.WorkflowDefinitions;

[Collection(nameof(FastEndpointsCollection))]
public class SaveWorkflowDefinitionTests : IAsyncLifetime
{
    private const string Path = "/workflow-definitions";
    private const string WritePermission = "workflows/definitions:write";
    private const string PublishPermission = "workflows/definitions:publish";
    private readonly MemoryWorkflowDefinitionStore _store = new(new MemoryStore<WorkflowDefinition>());
    private AuthorizationTestHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = await AuthorizationTestHost.StartAsync<WorkflowsApiFeature>(
            type => type.Namespace == "Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Post",
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
    public async Task MissingModelReturnsModelError()
    {
        using var response = await _host.SendAsync(HttpMethod.Post, Path, WritePermission,
            content: JsonContent.Create(new { publish = false }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEmpty(error.GetProperty("errors").GetProperty("model").EnumerateArray());
    }

    [Fact]
    public async Task CallerWithoutWritePermissionIsForbiddenEvenForBlankName()
    {
        using var response = await _host.SendAsync(HttpMethod.Post, Path, authenticated: true,
            content: JsonContent.Create(new { model = new { name = "" } }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WriterWithoutPublishPermissionCannotCreateOrChangeADefinitionByPublishing()
    {
        const string definitionId = "invoice";
        using var create = await _host.SendAsync(HttpMethod.Post, Path, WritePermission,
            content: JsonContent.Create(new { model = new { definitionId, name = "Invoice" } }));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        using var publishExisting = await _host.SendAsync(HttpMethod.Post, Path, WritePermission,
            content: JsonContent.Create(new { model = new { definitionId, name = "Changed" }, publish = true }));
        using var publishNew = await _host.SendAsync(HttpMethod.Post, Path, WritePermission,
            content: JsonContent.Create(new { model = new { name = "New" }, publish = true }));

        Assert.Equal(HttpStatusCode.Forbidden, publishExisting.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, publishNew.StatusCode);
        var draft = Assert.Single(await _store.FindManyAsync(new()));
        Assert.Equal(definitionId, draft.DefinitionId);
        Assert.Equal("Invoice", draft.Name);
        Assert.Equal(1, draft.Version);
        Assert.False(draft.IsPublished);
    }

    [Fact]
    public async Task WriterCanSaveDraftWithPublishedMetadataWhenPublishIsFalse()
    {
        using var response = await _host.SendAsync(HttpMethod.Post, Path, WritePermission,
            content: JsonContent.Create(new { model = new { name = "Invoice", isPublished = true }, publish = false }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var draft = Assert.Single(await _store.FindManyAsync(new()));
        Assert.False(draft.IsPublished);
    }

    [Fact]
    public async Task PublisherWithoutWritePermissionCannotSaveAndPublish()
    {
        using var response = await _host.SendAsync(HttpMethod.Post, Path, PublishPermission,
            content: JsonContent.Create(new { model = new { name = "Invoice" }, publish = true }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(await _store.FindManyAsync(new()));
    }

    [Fact]
    public async Task WriterWithPublishPermissionCanSaveAndPublish()
    {
        using var response = await _host.SendAsync(HttpMethod.Post, Path, $"{WritePermission},{PublishPermission}",
            content: JsonContent.Create(new { model = new { name = "Invoice" }, publish = true }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var definition = Assert.Single(await _store.FindManyAsync(new()));
        Assert.True(definition.IsPublished);
    }

    [Theory]
    [InlineData("   ", false)]
    [InlineData("", true)]
    [InlineData(null, false)]
    public async Task BlankNameReturnsFieldErrorAndLeavesDraftUnchanged(string? name, bool publish)
    {
        const string definitionId = "invoice";
        using var create = await _host.SendAsync(HttpMethod.Post, Path, WritePermission,
            content: JsonContent.Create(new { model = new { definitionId, name = "  Invoice  " } }));
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        using var update = await _host.SendAsync(HttpMethod.Post, Path, $"{WritePermission},{PublishPermission}",
            content: JsonContent.Create(new { model = new { definitionId, name }, publish }));

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        var error = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEmpty(error.GetProperty("errors").GetProperty("name").EnumerateArray());
        var draft = Assert.Single(await _store.FindManyAsync(new() { DefinitionId = definitionId }));
        Assert.Equal("Invoice", draft.Name);
        Assert.Equal(1, draft.Version);
        Assert.False(draft.IsPublished);
    }
}
