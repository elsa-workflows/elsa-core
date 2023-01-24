using Elsa.ActivityDefinitions.Services;
using FastEndpoints;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Version;

public class RevertVersion : EndpointWithoutRequest
{
    private readonly IActivityDefinitionManager _activityDefinitionManager;

    public RevertVersion(IActivityDefinitionManager activityDefinitionManager)
    {
        _activityDefinitionManager = activityDefinitionManager;
    }

    public override void Configure()
    {
        Post("activity-definitions/{definitionId}/revert/{version}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var definitionId = Route<string>("definitionId")!;
        var version = Route<int>("version");

        await _activityDefinitionManager.RevertVersionAsync(definitionId, version, ct);
        
        await SendOkAsync(ct);
    }
}