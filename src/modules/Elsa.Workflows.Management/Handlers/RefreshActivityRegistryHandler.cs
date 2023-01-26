using System.Diagnostics;
using Elsa.Mediator.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Implementations;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Handlers;

/// <summary>
/// Refreshes the <see cref="IActivityRegistry"/> for the <see cref="WorkflowDefinitionActivityProvider"/> provider whenever an <see cref="WorkflowDefinition"/> is published, retracted or deleted.
/// </summary>
public class RefreshActivityRegistryHandler :
    INotificationHandler<WorkflowDefinitionPublished>,
    INotificationHandler<WorkflowDefinitionRetracted>,
    INotificationHandler<WorkflowDefinitionDeleted>,
    INotificationHandler<WorkflowDefinitionsDeleted>
{
    private readonly IActivityRegistryPopulator _activityRegistryPopulator;
    public RefreshActivityRegistryHandler(IActivityRegistryPopulator activityRegistryPopulator) => _activityRegistryPopulator = activityRegistryPopulator;
    public async Task HandleAsync(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);
    }

    public async Task HandleAsync(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);
    public async Task HandleAsync(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);
    public async Task HandleAsync(WorkflowDefinitionsDeleted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);
    private async Task RefreshAsync(CancellationToken cancellationToken) => await _activityRegistryPopulator.PopulateRegistryAsync(typeof(WorkflowDefinitionActivityProvider), cancellationToken);
}