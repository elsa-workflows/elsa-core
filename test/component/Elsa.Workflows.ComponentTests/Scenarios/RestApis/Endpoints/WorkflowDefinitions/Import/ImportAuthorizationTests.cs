using System.Net;
using System.Text;
using System.Text.Json;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Materializers;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using WorkflowDefinitionEntity = Elsa.Workflows.Management.Entities.WorkflowDefinition;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.WorkflowDefinitions.Import;

public class ImportAuthorizationTests : AppComponentTest
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IWorkflowDefinitionsApi _client;

    public ImportAuthorizationTests(App app) : base(app)
    {
        _store = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        _client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();
    }

    [Fact]
    public async Task ImportExistingReadOnlyDefinition_ShouldReturnForbiddenAndLeaveStorageUnchanged()
    {
        var definitionId = $"readonly-import-{Guid.NewGuid():N}";
        await SaveDefinitionAsync(definitionId, "Original", isReadonly: true);

        var exception = await Assert.ThrowsAsync<ApiException>(() => _client.ImportAsync(CreateImportModel(definitionId, "Updated")));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        await AssertDefinitionUnchangedAsync(definitionId, "Original", isReadonly: true);
    }

    [Fact]
    public async Task ImportFilesWithReadOnlyTarget_ShouldReturnForbiddenAndLeaveStorageUnchanged()
    {
        var writableDefinitionId = $"writable-import-files-{Guid.NewGuid():N}";
        var readOnlyDefinitionId = $"readonly-import-files-{Guid.NewGuid():N}";
        await SaveDefinitionAsync(writableDefinitionId, "Writable Original");
        await SaveDefinitionAsync(readOnlyDefinitionId, "ReadOnly Original", isReadonly: true);

        await using var writableStream = CreateImportStream(writableDefinitionId, "Writable Updated");
        await using var readOnlyStream = CreateImportStream(readOnlyDefinitionId, "ReadOnly Updated");
        var files = new List<StreamPart>
        {
            new(writableStream, "writable.json", "application/json"),
            new(readOnlyStream, "readonly.json", "application/json")
        };

        var exception = await Assert.ThrowsAsync<ApiException>(() => _client.ImportFilesAsync(files));

        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        await AssertDefinitionUnchangedAsync(writableDefinitionId, "Writable Original");
        await AssertDefinitionUnchangedAsync(readOnlyDefinitionId, "ReadOnly Original", isReadonly: true);
    }

    private async Task SaveDefinitionAsync(string definitionId, string name, bool isReadonly = false)
    {
        await _store.SaveAsync(new WorkflowDefinitionEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            DefinitionId = definitionId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
            IsLatest = true,
            IsReadonly = isReadonly,
            MaterializerName = JsonWorkflowMaterializer.MaterializerName
        });
    }

    private async Task AssertDefinitionUnchangedAsync(string definitionId, string expectedName, bool isReadonly = false)
    {
        var definitions = (await _store.FindManyAsync(new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId
        })).ToList();

        var definition = Assert.Single(definitions);
        Assert.Equal(expectedName, definition.Name);
        Assert.Equal(isReadonly, definition.IsReadonly);
    }

    private static WorkflowDefinitionModel CreateImportModel(string definitionId, string name)
    {
        return new()
        {
            DefinitionId = definitionId,
            Name = name
        };
    }

    private static MemoryStream CreateImportStream(string definitionId, string name)
    {
        var json = JsonSerializer.Serialize(CreateImportModel(definitionId, name), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return new(Encoding.UTF8.GetBytes(json));
    }
}
