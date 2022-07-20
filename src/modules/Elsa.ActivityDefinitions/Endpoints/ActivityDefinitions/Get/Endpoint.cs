using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Services;
using Elsa.Api.Common;
using Elsa.Persistence.Common.Models;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Get;

public class Get : ProtectedEndpoint<Request, ActivityDefinition>
{
    private readonly IActivityDefinitionStore _activityDefinitionStore;

    public Get(IActivityDefinitionStore activityDefinitionStore)
    {
        _activityDefinitionStore = activityDefinitionStore;
    }

    public override void Configure()
    {
        Get("/activity-definitions/{definitionId}");
        ConfigureSecurity(SecurityConstants.Permissions, SecurityConstants.Policies, SecurityConstants.Roles);
    }

    public override async Task<ActivityDefinition> ExecuteAsync(Request req, CancellationToken ct)
    {
        var parsedVersionOptions = req.VersionOptions != null ? VersionOptions.FromString(req.VersionOptions) : VersionOptions.Published;
        var activityDefinition = await _activityDefinitionStore.FindByDefinitionIdAsync(req.DefinitionId, parsedVersionOptions, ct);
        return activityDefinition!;
    }
}