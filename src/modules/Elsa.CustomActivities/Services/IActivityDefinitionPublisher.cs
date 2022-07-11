using Elsa.CustomActivities.Entities;
using Elsa.Workflows.Persistence.Entities;

namespace Elsa.CustomActivities.Services
{
    public interface IActivityDefinitionPublisher
    {
        ActivityDefinition New();
        Task<ActivityDefinition?> PublishAsync(string definitionId, CancellationToken cancellationToken = default);
        Task<ActivityDefinition> PublishAsync(ActivityDefinition definition, CancellationToken cancellationToken = default);
        Task<ActivityDefinition?> RetractAsync(string definitionId, CancellationToken cancellationToken = default);
        Task<ActivityDefinition> RetractAsync(ActivityDefinition definition, CancellationToken cancellationToken = default);
        Task<ActivityDefinition?> GetDraftAsync(string definitionId, CancellationToken cancellationToken = default);
        Task<ActivityDefinition> SaveDraftAsync(ActivityDefinition definition, CancellationToken cancellationToken = default);
    }
}