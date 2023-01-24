using Elsa.ActivityDefinitions.Services;
using FastEndpoints;

namespace Elsa.ActivityDefinitions.Endpoints.ActivityDefinitions.Version;

public class DeleteVersion : EndpointWithoutRequest
{
    private readonly IActivityDefinitionManager _activityDefinitionManager;
    
    public DeleteVersion(IActivityDefinitionManager activityDefinitionManager)
    {
        _activityDefinitionManager = activityDefinitionManager;
    }

    public override void Configure()
    {
        Delete("activity-definitions/{definitionId}/version/{version}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var definitionId = Route<string>("definitionId")!;
        var version = Route<int>("version");
        
        var result = await _activityDefinitionManager.DeleteVersionAsync(definitionId, version, ct);
        if (!result)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(ct);
    }
}