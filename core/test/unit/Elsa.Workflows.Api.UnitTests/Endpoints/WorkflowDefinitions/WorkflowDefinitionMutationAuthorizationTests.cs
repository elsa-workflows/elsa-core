using System.Net;
using System.Net.Http.Json;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Testing.Shared.Authorization;
using Elsa.UnitTests.Shared;
using Elsa.Workflows.Api.Features;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Management.Stores;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.WorkflowDefinitions;

[Collection(nameof(FastEndpointsCollection))]
public class WorkflowDefinitionMutationAuthorizationTests : IAsyncLifetime
{
    private const string Permission = "workflows/definitions:delete,workflows/definitions/versions:delete,workflows/definitions/versions:revert,workflows/definitions:publish,workflows/definitions:retract";
    private readonly MemoryWorkflowDefinitionStore _store = new(new MemoryStore<WorkflowDefinition>());
    private AuthorizationTestHost _host = null!;
    private ManagementOptions _managementOptions = null!;

    public async Task InitializeAsync()
    {
        _host = await AuthorizationTestHost.StartAsync<WorkflowsApiFeature>(
            type => type.Namespace?.StartsWith("Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.", StringComparison.Ordinal) == true,
            services =>
            {
                services.AddElsa(elsa => elsa.UseWorkflowsApi());
                services.AddSingleton<IWorkflowDefinitionStore>(_store);
                services.PostConfigure<ManagementOptions>(options => _managementOptions = options);
            });
    }

    public async Task DisposeAsync()
    {
        FastEndpointsResolver.Reset();
        await _host.DisposeAsync();
    }

    [Theory]
    [InlineData("delete-definition", false)]
    [InlineData("delete-version", true)]
    [InlineData("publish", false)]
    [InlineData("retract", true)]
    public async Task MixedBatchRefusesEveryMutation(string operation, bool isSystem)
    {
        await SeedAsync("first", "first-id", published: operation == "retract");
        await SeedAsync("second", "second-id", published: operation == "retract", isSystem: isSystem, isReadonly: !isSystem);

        using var response = await SendBulkAsync(operation, "first", "second");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var definitions = (await _store.FindManyAsync(new())).ToList();
        Assert.Equal(2, definitions.Count);
        Assert.All(definitions, definition => Assert.Equal(operation == "retract", definition.IsPublished));
    }

    [Theory]
    [InlineData("delete-definition")]
    [InlineData("delete-version")]
    [InlineData("publish")]
    [InlineData("retract")]
    public async Task AllowedBatchCompletes(string operation)
    {
        await SeedAsync("first", "first-id", published: operation == "retract");

        using var response = await SendBulkAsync(operation, "first");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var definitions = (await _store.FindManyAsync(new())).ToList();
        if (operation.StartsWith("delete", StringComparison.Ordinal))
            Assert.Empty(definitions);
        else
            Assert.Equal(operation == "publish", Assert.Single(definitions).IsPublished);
    }

    [Fact]
    public async Task ReadOnlyModeForbidsBulkMutation()
    {
        _managementOptions.IsReadOnlyMode = true;
        await SeedAsync("first", "first-id");

        using var response = await SendBulkAsync("delete-definition", "first");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Single(await _store.FindManyAsync(new()));
    }

    [Theory]
    [InlineData("/workflow-definition-versions/protected")]
    [InlineData("/workflow-definitions/first/version/2")]
    [InlineData("/workflow-definitions/first")]
    public async Task ProtectedVersionCannotBeDeleted(string path)
    {
        await SeedAsync("first", "protected", isSystem: true, version: 2);
        await SeedAsync("first", "historical", version: 1, isLatest: false);

        using var response = await _host.SendAsync(HttpMethod.Delete, path, Permission);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(2, (await _store.FindManyAsync(new())).Count());
    }

    [Theory]
    [InlineData("/workflow-definition-versions/latest")]
    [InlineData("/workflow-definitions/first/version/2")]
    public async Task VersionDeletionCannotPromoteProtectedHistoricalVersion(string path)
    {
        await SeedAsync("first", "historical", version: 1, isLatest: false, isReadonly: true);
        await SeedAsync("first", "latest", version: 2);

        using var response = await _host.SendAsync(HttpMethod.Delete, path, Permission);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var definitions = (await _store.FindManyAsync(new())).ToList();
        Assert.Equal("latest", Assert.Single(definitions, definition => definition.IsLatest).Id);
        Assert.Equal(2, definitions.Count);
    }

    [Fact]
    public async Task RevertCannotUnmarkProtectedLatestVersion()
    {
        await SeedAsync("first", "historical", version: 1, isLatest: false);
        await SeedAsync("first", "latest", version: 2, isReadonly: true);

        using var response = await _host.SendAsync(HttpMethod.Post, "/workflow-definitions/first/revert/1", Permission);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var definitions = (await _store.FindManyAsync(new())).ToList();
        Assert.Equal("latest", Assert.Single(definitions, definition => definition.IsLatest).Id);
        Assert.Equal(2, definitions.Count);
    }

    [Fact]
    public async Task BulkPublishCannotUnpublishProtectedVersionOfTheSameDefinition()
    {
        await SeedAsync("first", "published", published: true, version: 1, isLatest: false, isSystem: true);
        await SeedAsync("first", "draft", version: 2);

        using var response = await SendBulkAsync("publish", "first");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var definitions = (await _store.FindManyAsync(new())).ToList();
        Assert.Equal("published", Assert.Single(definitions, definition => definition.IsPublished).Id);
        Assert.Equal("draft", Assert.Single(definitions, definition => definition.IsLatest).Id);
    }

    private Task<HttpResponseMessage> SendBulkAsync(string operation, params string[] ids)
    {
        var (path, content) = operation switch
        {
            "delete-definition" => ("/bulk-actions/delete/workflow-definitions/by-definition-id", JsonContent.Create(new { definitionIds = ids })),
            "delete-version" => ("/bulk-actions/delete/workflow-definitions/by-id", JsonContent.Create(new { ids = ids.Select(id => $"{id}-id") })),
            _ => ($"/bulk-actions/{operation}/workflow-definitions/by-definition-ids", JsonContent.Create(new { definitionIds = ids }))
        };

        return _host.SendAsync(HttpMethod.Post, path, Permission, content: content);
    }

    private Task SeedAsync(string definitionId, string id, bool published = false, int version = 1, bool isLatest = true,
        bool isSystem = false, bool isReadonly = false) => _store.SaveAsync(new WorkflowDefinition
    {
        Id = id,
        DefinitionId = definitionId,
        Name = definitionId,
        Version = version,
        IsLatest = isLatest,
        IsPublished = published,
        IsSystem = isSystem,
        IsReadonly = isReadonly,
        MaterializerName = JsonWorkflowMaterializer.MaterializerName,
        StringData = """{"type":"Elsa.Sequence","activities":[]}"""
    });
}
