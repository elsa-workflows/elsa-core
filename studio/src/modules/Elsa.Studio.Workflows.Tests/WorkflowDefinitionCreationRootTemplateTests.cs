using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Services;
using Refit;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class WorkflowDefinitionCreationRootTemplateTests
{
    private readonly CapturingWorkflowDefinitionsApi _api = new();
    private readonly RemoteWorkflowDefinitionService _service;

    public WorkflowDefinitionCreationRootTemplateTests()
    {
        var backendApiClientProvider = new TestBackendApiClientProvider(_api);
        var identityGenerator = new TestIdentityGenerator();
        var workflowRootActivityTemplateProvider = new DefaultWorkflowRootActivityTemplateProvider();
        var mediator = new TestMediator();

        _service = new(backendApiClientProvider, identityGenerator, workflowRootActivityTemplateProvider, mediator);
    }

    [Fact]
    public async Task CreateNewDefinitionAsyncUsesFlowchartByDefault()
    {
        await _service.CreateNewDefinitionAsync("Test workflow", "Description");

        AssertSavedRoot(DefaultWorkflowRootActivityTemplateProvider.FlowchartKey, "Flowchart1");
        Assert.Null(_api.SaveRequest!.Model.Root!["activities"]);
    }

    [Theory]
    [InlineData(DefaultWorkflowRootActivityTemplateProvider.SequenceKey, "Sequence1", "activities")]
    [InlineData(DefaultWorkflowRootActivityTemplateProvider.StateMachineKey, "StateMachine1", "states", "transitions")]
    public async Task CreateNewDefinitionAsyncUsesSelectedRootTemplate(string templateKey, string expectedName, params string[] expectedArrayProperties)
    {
        await _service.CreateNewDefinitionAsync("Test workflow", "Description", templateKey);

        AssertSavedRoot(templateKey, expectedName);

        foreach (var propertyName in expectedArrayProperties)
            Assert.Empty(_api.SaveRequest!.Model.Root![propertyName]!.AsArray());
    }

    [Fact]
    public async Task CreateNewDefinitionAsyncFallsBackToFlowchartForUnknownTemplate()
    {
        await _service.CreateNewDefinitionAsync("Test workflow", "Description", "Unknown");

        AssertSavedRoot(DefaultWorkflowRootActivityTemplateProvider.FlowchartKey, "Flowchart1");
    }

    private void AssertSavedRoot(string expectedType, string expectedName)
    {
        var root = _api.SaveRequest!.Model.Root!;

        Assert.Equal("activity-1", root["id"]!.GetValue<string>());
        Assert.Equal(expectedType, root["type"]!.GetValue<string>());
        Assert.Equal(1, root["version"]!.GetValue<int>());
        Assert.Equal(expectedName, root["name"]!.GetValue<string>());
    }

    private class CapturingWorkflowDefinitionsApi : IWorkflowDefinitionsApi
    {
        public SaveWorkflowDefinitionRequest? SaveRequest { get; private set; }

        public Task<SaveWorkflowDefinitionResponse> SaveAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            SaveRequest = request;

            var workflowDefinition = new WorkflowDefinition
            {
                Name = request.Model.Name,
                Description = request.Model.Description,
                Root = request.Model.Root!
            };

            return Task.FromResult(new SaveWorkflowDefinitionResponse(workflowDefinition, false, 0));
        }

        public Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowDefinition?> GetByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowDefinition?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ListResponse<WorkflowDefinition>> GetManyByIdAsync(ICollection<string> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ActivityNode?> GetSubgraphAsync(string id, string? parentNodeId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GetPathSegmentsResponse?> GetPathSegmentsGetAsync(string id, string? childNodeId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CountWorkflowDefinitionsResponse> CountAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GetConsumingWorkflowDefinitionsResponse> GetConsumersAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GetIsNameUniqueResponse> GetIsNameUniqueAsync(string name, string? definitionId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteVersionAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SaveWorkflowDefinitionResponse> PublishAsync(string definitionId, PublishWorkflowDefinitionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowDefinition> RetractAsync(string definitionId, RetractWorkflowDefinitionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BulkDeleteWorkflowDefinitionsResponse> BulkDeleteAsync(BulkDeleteWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BulkDeleteWorkflowDefinitionsResponse> BulkDeleteVersionsAsync(BulkDeleteWorkflowDefinitionVersionsRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BulkPublishWorkflowDefinitionsResponse> BulkPublishAsync(BulkPublishWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BulkRetractWorkflowDefinitionsResponse> BulkRetractAsync(BulkRetractWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IApiResponse<Stream>> ExportAsync(string definitionId, VersionOptions? versionOptions = null, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IApiResponse<Stream>> BulkExportAsync(BulkExportWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowDefinition> ImportAsync(WorkflowDefinitionModel model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ImportFilesResponse> ImportFilesAsync(List<StreamPart> files, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UpdateConsumingWorkflowReferencesResponse> UpdateReferencesAsync(string definitionId, UpdateConsumingWorkflowReferencesRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowDefinitionSummary> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private class TestBackendApiClientProvider(IWorkflowDefinitionsApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://localhost");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            if (typeof(T) == typeof(IWorkflowDefinitionsApi))
                return ValueTask.FromResult((T)api);

            throw new NotSupportedException();
        }
    }

    private class TestIdentityGenerator : IIdentityGenerator
    {
        public string GenerateId() => "activity-1";
    }

    private class TestMediator : IMediator
    {
        public void Subscribe<TNotification, THandler>(THandler handler)
            where TNotification : INotification
            where THandler : INotificationHandler<TNotification>
        {
        }

        public void Unsubscribe<TNotification, THandler>(THandler handler)
            where TNotification : INotification
            where THandler : INotificationHandler<TNotification>
        {
        }

        public void Unsubscribe(INotificationHandler handler)
        {
        }

        public Task NotifyAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }
}
