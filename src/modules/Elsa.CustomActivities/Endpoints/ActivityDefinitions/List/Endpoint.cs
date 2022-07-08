using Elsa.CustomActivities.Services;
using FastEndpoints;

namespace Elsa.CustomActivities.Endpoints.ActivityDefinitions.List;

public class List : EndpointWithoutRequest<Response>
{
    private readonly IActivityDefinitionStore _activityDefinitionStore;

    public List(IActivityDefinitionStore activityDefinitionStore)
    {
        _activityDefinitionStore = activityDefinitionStore;
    }
    
    public override void Configure()
    {
        Routes("/api/custom-activities/activity-definitions");
        Verbs(Http.GET);
    }

    public override async Task<Response> ExecuteAsync(CancellationToken ct)
    {
        var definitions = _activityDefinitionStore.
        return new Response();
    }
}