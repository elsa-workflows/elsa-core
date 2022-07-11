using Elsa.CustomActivities.Entities;
using Elsa.CustomActivities.Services;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Serialization;
using FastEndpoints;

namespace Elsa.CustomActivities.Endpoints.ActivityDefinitions.Get;

public class Get : Endpoint<Request, ActivityDefinition>
{
    private readonly IActivityDefinitionStore _activityDefinitionStore;

    public Get(IActivityDefinitionStore activityDefinitionStore)
    {
        _activityDefinitionStore = activityDefinitionStore;
    }

    public override void Configure()
    {
        Routes("/api/custom-activities/activity-definitions/{definitionId}");
        Verbs(Http.GET);
    }

    public override async Task<ActivityDefinition> ExecuteAsync(Request req, CancellationToken ct)
    {
        var parsedVersionOptions = req.VersionOptions != null ? VersionOptions.FromString(req.VersionOptions) : VersionOptions.Published;
        var activityDefinition = await _activityDefinitionStore.FindByDefinitionIdAsync(req.DefinitionId, parsedVersionOptions, ct);
        return activityDefinition!;
    }
}