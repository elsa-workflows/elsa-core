using Elsa.CustomActivities.Entities;
using Elsa.CustomActivities.Models;
using Elsa.CustomActivities.Services;
using Elsa.Persistence.Common.Extensions;
using Elsa.Persistence.Common.Implementations;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Persistence.Models;

namespace Elsa.CustomActivities.Implementations;

public class MemoryActivityDefinitionStore : IActivityDefinitionStore
{
    private readonly MemoryStore<ActivityDefinition> _activityDefinitionStore;

    public MemoryActivityDefinitionStore(MemoryStore<ActivityDefinition> activityDefinitionStore)
    {
        _activityDefinitionStore = activityDefinitionStore;
    }
    
    public Task<Page<ActivityDefinitionSummary>> ListSummariesAsync(VersionOptions? versionOptions = default, string? materializerName = default, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        var query = _activityDefinitionStore.List().AsQueryable();

        if (versionOptions != null) query = query.WithVersion(versionOptions.Value);
        if (!string.IsNullOrWhiteSpace(materializerName)) query = query.Where(x => x.MaterializerName == materializerName);

        var page = query.Paginate(x => ActivityDefinitionSummary.FromDefinition(x), pageArgs);
        return Task.FromResult(page);
    }
}