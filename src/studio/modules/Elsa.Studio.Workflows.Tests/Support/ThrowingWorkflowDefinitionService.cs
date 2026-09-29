using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>
/// An <see cref="IWorkflowDefinitionService"/> base whose every member throws by default, for stubs that only
/// need a handful of members to behave. A backend round trip is not a failure the component would report -- it
/// would simply be slower and, on a disconnected circuit, silently empty -- so the base makes it loud. Members are
/// virtual so a derived stub can override the ones it needs to control.
/// </summary>
internal class ThrowingWorkflowDefinitionServiceBase : IWorkflowDefinitionService
{
    private static Exception Unexpected([System.Runtime.CompilerServices.CallerMemberName] string? member = null) =>
        new NotSupportedException($"'{member}' asked the backend for a node the designer should already hold.");

    public virtual Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<IEnumerable<WorkflowDefinition>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<ActivityNode?> FindSubgraphAsync(string id, string? parentNodeId = null, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<GetPathSegmentsResponse?> GetPathSegmentsAsync(string id, string? childNodeId = null, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<bool> DeleteAsync(string definitionId, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<bool> DeleteVersionAsync(WorkflowDefinitionVersion workflowDefinitionVersion, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<SaveWorkflowDefinitionResponse> PublishAsync(string definitionId, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(string definitionId, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<long> BulkDeleteAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<long> BulkDeleteVersionsAsync(IEnumerable<WorkflowDefinitionVersion> workflowDefinitionVersions, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<BulkPublishWorkflowDefinitionsResponse> BulkPublishAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<BulkRetractWorkflowDefinitionsResponse> BulkRetractAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = null, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description = null, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description, string? rootActivityTemplateKey, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<FileDownload> ExportDefinitionAsync(string definitionId, VersionOptions? versionOptions = null, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<FileDownload> BulkExportDefinitionsAsync(IEnumerable<string> ids, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<UpdateConsumingWorkflowReferencesResponse> UpdateReferencesAsync(string definitionId, CancellationToken cancellationToken = default) => throw Unexpected();
    public virtual Task<ExecuteWorkflowResult> ExecuteAsync(string definitionId, ExecuteWorkflowDefinitionRequest? request, CancellationToken cancellationToken = default) => throw Unexpected();
}

/// <summary>
/// An <see cref="IWorkflowDefinitionService"/> stub whose every member throws, for tests asserting that a component
/// resolves everything it needs from the graph it already holds.
/// </summary>
internal sealed class ThrowingWorkflowDefinitionService : ThrowingWorkflowDefinitionServiceBase;
