using Elsa.ActivityDefinitions.Entities;

namespace Elsa.ActivityDefinitions.Services;

public interface IActivityDefinitionManager
{
    Task<bool> DeleteVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default);
    Task<ActivityDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default);
}