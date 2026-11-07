using System.Net;
using System.Text;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using WorkflowDefinitionEntity = Elsa.Workflows.Management.Entities.WorkflowDefinition;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.WorkflowDefinitions.Import;

public class ImportFilesValidationTests : AppComponentTest
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IActivitySerializer _activitySerializer;
    private readonly IApiSerializer _apiSerializer;
    private readonly IWorkflowDefinitionsApi _client;

    public ImportFilesValidationTests(App app) : base(app)
    {
        _store = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        _activitySerializer = Scope.ServiceProvider.GetRequiredService<IActivitySerializer>();
        _apiSerializer = Scope.ServiceProvider.GetRequiredService<IApiSerializer>();
        _client = WorkflowServer.CreateApiClient<IWorkflowDefinitionsApi>();
    }

    [Fact]
    public async Task MalformedJsonAfterValidUpdate_ShouldReturnSafeValidationErrorAndLeaveStorageUnchanged()
    {
        var definitionId = $"invalid-import-files-{Guid.NewGuid():N}";
        var original = new WorkflowDefinitionEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            DefinitionId = definitionId,
            Name = "Original",
            CreatedAt = DateTimeOffset.UtcNow,
            IsLatest = true,
            MaterializerName = JsonWorkflowMaterializer.MaterializerName,
            StringData = _activitySerializer.Serialize(new Sequence())
        };
        await _store.SaveAsync(original);
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId };
        var originalJson = _apiSerializer.Serialize(Assert.Single(await _store.FindManyAsync(filter)));

        var updateJson = _apiSerializer.Serialize(new WorkflowDefinitionModel { DefinitionId = definitionId, Name = "Updated" });
        await using var updateStream = new MemoryStream(Encoding.UTF8.GetBytes(updateJson));
        await using var invalidStream = new MemoryStream(Encoding.UTF8.GetBytes("{ not json"));
        var files = new List<StreamPart>
        {
            new(updateStream, "valid.json", "application/json"),
            new(invalidStream, "bad.json", "application/json")
        };

        var exception = await Assert.ThrowsAsync<ValidationApiException>(() => _client.ImportFilesAsync(files));

        AssertValidationResponse(exception, "bad.json is not a valid workflow definition.");
        var definitions = await _store.FindManyAsync(filter);
        var stored = Assert.Single(definitions);
        Assert.Equal(originalJson, _apiSerializer.Serialize(stored));
    }

    [Fact]
    public async Task NonZipUpload_ShouldReturnSafeValidationErrorNamingUpload()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not a zip"));
        var files = new List<StreamPart> { new(stream, "bundle.zip", "application/octet-stream") };

        var exception = await Assert.ThrowsAsync<ValidationApiException>(() => _client.ImportFilesAsync(files));

        AssertValidationResponse(exception, "bundle.zip is not a valid workflow archive.");
    }

    private static void AssertValidationResponse(ValidationApiException exception, string expectedMessage)
    {
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        var response = Assert.IsType<ProblemDetails>(exception.Content);
        var errors = Assert.Single(response.Errors);
        Assert.Equal("generalErrors", errors.Key);
        Assert.Equal(expectedMessage, Assert.Single(errors.Value));
        Assert.Equal(["message", "statusCode"], response.Extensions.Keys.Order());
        Assert.Equal("400", response.Extensions["statusCode"].ToString());
        Assert.Equal("One or more errors occurred!", response.Extensions["message"].ToString());
        Assert.Equal("about:blank", response.Type);
        Assert.Null(response.Title);
        Assert.Null(response.Detail);
        Assert.Null(response.Instance);
    }
}
