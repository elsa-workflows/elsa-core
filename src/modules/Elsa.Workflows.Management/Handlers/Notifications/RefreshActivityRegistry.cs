using Elsa.Mediator.Contracts;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Handlers.Notifications;

/// <summary>
/// Refreshes the <see cref="IActivityRegistry"/> for the <see cref="WorkflowDefinitionActivityProvider"/> provider whenever an <see cref="WorkflowDefinition"/> is published, retracted or deleted.
/// </summary>
[PublicAPI]
public class RefreshActivityRegistry(
    IWorkflowDefinitionActivityRegistryUpdater workflowDefinitionActivityRegistryUpdater,
    IWorkflowDefinitionRegistryGenerationStore generationStore,
    ITenantAccessor tenantAccessor) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionVersionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsUpdated>,
    INotificationHandler<WorkflowDefinitionDraftSaved>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        await UpdateDefinition(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity);
        await generationStore.IncrementAsync(notification.WorkflowDefinition.TenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken)
    { 
        await UpdateDefinition(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity);
        await generationStore.IncrementAsync(notification.WorkflowDefinition.TenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionRetracted notification, CancellationToken cancellationToken)
    { 
        await UpdateDefinition(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity);
        await generationStore.IncrementAsync(notification.WorkflowDefinition.TenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
    { 
        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionFromRegistry(notification.DefinitionId);
        await IncrementTenantAndAgnosticGenerationsAsync(tenantAccessor.TenantId, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (string id in notification.DefinitionIds)
        {
            workflowDefinitionActivityRegistryUpdater.RemoveDefinitionFromRegistry(id);
        }

        await IncrementTenantAndAgnosticGenerationsAsync(tenantAccessor.TenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken)
    { 
        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(notification.WorkflowDefinition.Id);
        await generationStore.IncrementAsync(notification.WorkflowDefinition.TenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken)
    {
        foreach (string id in notification.Ids)
        {
            workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(id);
        }

        await IncrementTenantAndAgnosticGenerationsAsync(tenantAccessor.TenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsUpdated notification, CancellationToken cancellationToken)
    {
        var definitions = notification.WorkflowDefinitions.ToList();
        foreach (var definition in definitions)
        {
            await UpdateDefinition(definition.Id, definition.Options.UsableAsActivity);
        }

        foreach (var tenantId in definitions.Select(x => x.TenantId).Distinct())
            await generationStore.IncrementAsync(tenantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionDraftSaved notification, CancellationToken cancellationToken)
    {
        await UpdateDefinition(notification.WorkflowDefinition.Id, notification.WorkflowDefinition.Options.UsableAsActivity);
        await generationStore.IncrementAsync(notification.WorkflowDefinition.TenantId, cancellationToken);
    }

    private Task UpdateDefinition(string id, bool? usableAsActivity)
    {
        // Once a workflow has been published, it should remain in the activity registry unless no longer being marked as an activity.
        if (usableAsActivity.GetValueOrDefault())
            return workflowDefinitionActivityRegistryUpdater.AddToRegistry(id);

        workflowDefinitionActivityRegistryUpdater.RemoveDefinitionVersionFromRegistry(id);
        return Task.CompletedTask;
    }

    private async Task IncrementTenantAndAgnosticGenerationsAsync(string? tenantId, CancellationToken cancellationToken)
    {
        var normalizedTenantId = tenantId.NormalizeTenantId();
        if (normalizedTenantId != Tenant.AgnosticTenantId)
            await generationStore.IncrementAsync(Tenant.AgnosticTenantId, cancellationToken);

        await generationStore.IncrementAsync(normalizedTenantId, cancellationToken);
    }
}
