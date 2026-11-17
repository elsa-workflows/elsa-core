using System.Net;
using System.Text;
using System.Text.Json;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Testing.Shared.Authorization;
using Elsa.UnitTests.Shared;
using Elsa.Workflows.Api.Features;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.WorkflowInstances;

[Collection(nameof(FastEndpointsCollection))]
public class WorkflowInstanceRestoreTests : IAsyncLifetime
{
    private const string Route = "/bulk-actions/import/workflow-instances";
    private const string Restore = "workflows/instances:restore";
    private readonly MemoryWorkflowDefinitionStore _definitions = new(new MemoryStore<WorkflowDefinition>());
    private readonly MemoryWorkflowInstanceStore _instances = new(new MemoryStore<WorkflowInstance>());
    private AuthorizationTestHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = await AuthorizationTestHost.StartAsync<WorkflowsApiFeature>(
            type => type.Namespace == "Elsa.Workflows.Api.Endpoints.WorkflowInstances.Import",
            services =>
            {
                services.AddElsa(elsa => elsa.UseWorkflowsApi());
                services.AddSingleton<IWorkflowDefinitionStore>(_definitions);
                services.AddSingleton<IWorkflowInstanceStore>(_instances);
            });

        await _definitions.SaveAsync(new WorkflowDefinition { Id = "version-1", DefinitionId = "definition", Version = 1 });
    }

    public async Task DisposeAsync()
    {
        FastEndpointsResolver.Reset();
        await _host.DisposeAsync();
    }

    [Fact]
    public async Task InstanceWriteDoesNotAuthorizeRestore()
    {
        using var upload = CreateUpload(Archive("archived"));
        using var result = await _host.SendAsync(HttpMethod.Post, Route, "workflows/instances:write", content: upload);

        Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Equal(0, await _instances.CountAsync(new()));
    }

    [Fact]
    public async Task RestoreValidatesWholeBatchAndRejectsExecutableState()
    {
        var valid = Archive("archived");
        var runnable = Archive("runnable", state =>
        {
            state.Status = WorkflowStatus.Running;
            state.SubStatus = WorkflowSubStatus.Interrupted;
        });
        using var upload = CreateUpload(valid, runnable);
        using var result = await _host.SendAsync(HttpMethod.Post, Route, Restore, content: upload);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(0, await _instances.CountAsync(new()));
    }

    [Theory]
    [InlineData("foreign-tenant", null, false)]
    [InlineData(null, "different-version", false)]
    [InlineData(null, null, true)]
    public async Task RestoreRefusesTenantVersionAndBookmarkMismatch(string? tenant, string? version, bool hasBookmark)
    {
        using var upload = CreateUpload(Archive("archived", state =>
        {
            if (version != null)
                state.DefinitionVersionId = version;
        }, tenant, hasBookmark));
        using var result = await _host.SendAsync(HttpMethod.Post, Route, Restore, content: upload);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(0, await _instances.CountAsync(new()));
    }

    [Fact]
    public async Task RestoreCreatesTerminalInstanceAndRefusesReplacement()
    {
        using var upload = CreateUpload(Archive("archived"));
        using var result = await _host.SendAsync(HttpMethod.Post, Route, Restore, content: upload);
        Assert.True(result.IsSuccessStatusCode, await result.Content.ReadAsStringAsync());
        var instance = Assert.IsType<WorkflowInstance>(await _instances.FindAsync(new() { Id = "archived" }));
        Assert.Equal(WorkflowStatus.Finished, instance.Status);
        Assert.Equal(WorkflowSubStatus.Finished, instance.SubStatus);
        Assert.Empty(instance.WorkflowState.Bookmarks);

        using var duplicate = CreateUpload(Archive("archived", state => state.Name = "Replacement"));
        using var conflict = await _host.SendAsync(HttpMethod.Post, Route, Restore, content: duplicate);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Null((await _instances.FindAsync(new() { Id = "archived" }))!.Name);
    }

    private static string Archive(string instanceId, Action<WorkflowState>? configure = null, string? tenantId = null, bool hasBookmark = false)
    {
        var state = new WorkflowState
        {
            Id = instanceId,
            DefinitionId = "definition",
            DefinitionVersionId = "version-1",
            DefinitionVersion = 1,
            Status = WorkflowStatus.Finished,
            SubStatus = WorkflowSubStatus.Finished
        };
        configure?.Invoke(state);
        var workflowState = JsonSerializer.SerializeToElement(state, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var bookmarks = hasBookmark ? JsonSerializer.SerializeToElement(new[] { new { id = "fixture" } }) : (JsonElement?)null;
        return JsonSerializer.Serialize(new { WorkflowState = workflowState, TenantId = tenantId, Bookmarks = bookmarks });
    }

    private static MultipartFormDataContent CreateUpload(params string[] archives)
    {
        var upload = new MultipartFormDataContent();
        for (var index = 0; index < archives.Length; index++)
            upload.Add(new StringContent(archives[index], Encoding.UTF8, "application/json"), "files", $"archive-{index}.json");
        return upload;
    }
}
