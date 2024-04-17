using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Handlers;

/// <summary>
/// Refreshes the <see cref="IActivityRegistry"/> for the <see cref="WorkflowDefinitionActivityProvider"/> provider whenever an <see cref="WorkflowDefinition"/> is published, retracted or deleted.
/// </summary>
[PublicAPI]
public class RefreshActivityRegistry(IActivityRegistryPopulator activityRegistryPopulator) :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionCreated>,
    INotificationHandler<WorkflowDefinitionVersionDeleted>,
    INotificationHandler<WorkflowDefinitionVersionsDeleted>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        await activityRegistryPopulator.AddToRegistry(typeof(WorkflowDefinitionActivityProvider), notification.WorkflowDefinition.Id);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionCreated notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionDeleted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionVersionsDeleted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await activityRegistryPopulator.PopulateRegistryAsync(typeof(WorkflowDefinitionActivityProvider), cancellationToken);
    }
}