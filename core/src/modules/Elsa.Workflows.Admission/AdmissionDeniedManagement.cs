using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Admission;

// Public definition mutation services are deliberately unavailable in the first supported
// worker host. Bootstrap uses its private audited built-in publisher after ledger withdrawal.
internal sealed class AdmissionDeniedManagement : IWorkflowDefinitionPublisher, IWorkflowDefinitionManager
{
    private static InvalidOperationException Denied() => new("Generic definition management is unsupported in the isolated admission host.");
    public WorkflowDefinition New(IActivity? root = null) => throw Denied();
    public Task<WorkflowDefinition> NewAsync(IActivity? root = null, CancellationToken cancellationToken = default) => throw Denied();
    public Task<PublishWorkflowDefinitionResult> PublishAsync(string definitionId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<PublishWorkflowDefinitionResult> PublishAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) => throw Denied();
    public Task<WorkflowDefinition?> RetractAsync(string definitionId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<WorkflowDefinition> RetractAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) => throw Denied();
    public Task<WorkflowDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default) => throw Denied();
    public Task<WorkflowDefinition?> GetDraftAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default) => throw Denied();
    public Task<WorkflowDefinition> SaveDraftAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default) => throw Denied();
    public Task<long> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) => throw Denied();
    public Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken = default) => throw Denied();
    public Task<bool> DeleteVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default) => throw Denied();
    public Task<bool> DeleteVersionAsync(WorkflowDefinition definitionToDelete, CancellationToken cancellationToken) => throw Denied();
    public Task<long> BulkDeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw Denied();
    public Task<long> BulkDeleteByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => throw Denied();
}
