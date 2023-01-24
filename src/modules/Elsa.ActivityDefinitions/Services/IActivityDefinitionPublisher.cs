using Elsa.ActivityDefinitions.Entities;

namespace Elsa.ActivityDefinitions.Services;

public interface IActivityDefinitionPublisher
{
    ActivityDefinition New();
    Task<ActivityDefinition?> PublishAsync(string definitionId, CancellationToken cancellationToken = default);
    Task<ActivityDefinition> PublishAsync(ActivityDefinition definition, CancellationToken cancellationToken = default);
    Task<ActivityDefinition?> RetractAsync(string definitionId, CancellationToken cancellationToken = default);
    Task<ActivityDefinition> RetractAsync(ActivityDefinition definition, CancellationToken cancellationToken = default);
    Task<ActivityDefinition?> GetDraftAsync(string definitionId, int? version = null, CancellationToken cancellationToken = default);
    Task<ActivityDefinition> SaveDraftAsync(ActivityDefinition definition, CancellationToken cancellationToken = default);
}