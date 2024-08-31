using Elsa.Agents.Activities.ActivityProviders;
using Elsa.Agents.Persistence.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using JetBrains.Annotations;

namespace Elsa.Agents.Activities.Handlers;

[UsedImplicitly]
public class RefreshActivityRegistry(IActivityRegistry activityRegistry, AgentActivityProvider agentActivityProvider) : 
    INotificationHandler<AgentDefinitionCreated>,
    INotificationHandler<AgentDefinitionUpdated>,
    INotificationHandler<AgentDefinitionDeleted>,
    INotificationHandler<AgentDefinitionsDeletedInBulk>
{
    public Task HandleAsync(AgentDefinitionCreated notification, CancellationToken cancellationToken) => RefreshRegistryAsync(cancellationToken);
    public Task HandleAsync(AgentDefinitionUpdated notification, CancellationToken cancellationToken) => RefreshRegistryAsync(cancellationToken);
    public Task HandleAsync(AgentDefinitionDeleted notification, CancellationToken cancellationToken) => RefreshRegistryAsync(cancellationToken);
    public Task HandleAsync(AgentDefinitionsDeletedInBulk notification, CancellationToken cancellationToken) => RefreshRegistryAsync(cancellationToken);
    private Task RefreshRegistryAsync(CancellationToken cancellationToken) => activityRegistry.RefreshDescriptorsAsync([agentActivityProvider], cancellationToken);
}