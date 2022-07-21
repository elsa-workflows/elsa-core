using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Implementations;
using Elsa.ActivityDefinitions.Notifications;
using Elsa.Mediator.Services;
using Elsa.Workflows.Management.Services;

namespace Elsa.ActivityDefinitions.Handlers;

/// <summary>
/// Refreshes the <see cref="IActivityRegistry"/> for the <see cref="ActivityDefinitionActivityProvider"/> provider whenever an <see cref="ActivityDefinition"/> is published, retracted or deleted.
/// </summary>
public class RefreshActivityRegistryHandler :
    INotificationHandler<ActivityDefinitionPublished>,
    INotificationHandler<ActivityDefinitionRetracted>,
    INotificationHandler<ActivityDefinitionDeleted>,
    INotificationHandler<ActivityDefinitionsDeleted>
{
    private readonly IActivityRegistryPopulator _activityRegistryPopulator;
    public RefreshActivityRegistryHandler(IActivityRegistryPopulator activityRegistryPopulator) => _activityRegistryPopulator = activityRegistryPopulator;
    public async Task HandleAsync(ActivityDefinitionPublished notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);
    public async Task HandleAsync(ActivityDefinitionRetracted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);
    public async Task HandleAsync(ActivityDefinitionDeleted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);
    public async Task HandleAsync(ActivityDefinitionsDeleted notification, CancellationToken cancellationToken) => await RefreshAsync(cancellationToken);
    private async Task RefreshAsync(CancellationToken cancellationToken) => await _activityRegistryPopulator.PopulateRegistryAsync(typeof(ActivityDefinitionActivityProvider), cancellationToken);
}