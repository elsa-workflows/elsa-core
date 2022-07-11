using Elsa.CustomActivities.Entities;
using Elsa.CustomActivities.Models;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Persistence.Models;

namespace Elsa.CustomActivities.Services;

public interface IActivityDefinitionStore
{
    Task<Page<ActivityDefinitionSummary>> ListSummariesAsync(
        VersionOptions? versionOptions = default,
        PageArgs? pageArgs = default,
        CancellationToken cancellationToken = default);

    Task<ActivityDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    Task<IEnumerable<ActivityDefinition>> FindLatestAndPublishedByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
    Task SaveAsync(ActivityDefinition record, CancellationToken cancellationToken = default);
}