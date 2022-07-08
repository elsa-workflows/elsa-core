using Elsa.CustomActivities.Models;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Persistence.Models;

namespace Elsa.CustomActivities.Services;

public interface IActivityDefinitionStore
{
    Task<Page<ActivityDefinitionSummary>> ListSummariesAsync(
        VersionOptions? versionOptions = default,
        string? materializerName = default,
        PageArgs? pageArgs = default,
        CancellationToken cancellationToken = default);
}