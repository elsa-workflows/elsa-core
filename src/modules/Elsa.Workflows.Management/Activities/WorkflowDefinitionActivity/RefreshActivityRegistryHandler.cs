using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Notifications;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Activities.WorkflowDefinitionActivity;

/// <summary>
/// Refreshes the <see cref="IActivityRegistry"/> for the <see cref="WorkflowDefinitionActivityProvider"/> provider whenever an <see cref="WorkflowDefinition"/> is published, retracted or deleted.
/// </summary>
[PublicAPI]
public class RefreshActivityRegistryHandler :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>,
    INotificationHandler<WorkflowDefinitionCreated>
{
    private readonly IActivityRegistryPopulator _activityRegistryPopulator;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshActivityRegistryHandler"/> class.
    /// </summary>
    public RefreshActivityRegistryHandler(IActivityRegistryPopulator activityRegistryPopulator) => _activityRegistryPopulator = activityRegistryPopulator;
    
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);

    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionCreated notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);
    
    private async Task RefreshAsync(CancellationToken cancellationToken) => await _activityRegistryPopulator.PopulateRegistryAsync(typeof(WorkflowDefinitionActivityProvider), cancellationToken);
}