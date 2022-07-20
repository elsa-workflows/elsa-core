using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Models;
using Elsa.Persistence.Common.Models;

namespace Elsa.ActivityDefinitions.Services;

public interface IActivityDefinitionStore
{
    Task<Page<ActivityDefinition>> ListAsync(
        VersionOptions? versionOptions = default,
        PageArgs? pageArgs = default,
        CancellationToken cancellationToken = default);

    Task<Page<ActivityDefinitionSummary>> ListSummariesAsync(
        VersionOptions? versionOptions = default,
        PageArgs? pageArgs = default,
        CancellationToken cancellationToken = default);

    Task<ActivityDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    Task<ActivityDefinition?> FindByDefinitionVersionIdAsync(string definitionVersionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActivityDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
    Task SaveAsync(ActivityDefinition record, CancellationToken cancellationToken = default);
}