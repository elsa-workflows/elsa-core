using FastEndpoints;

namespace Elsa.CustomActivities.Endpoints.ActivityDefinitions.List;

public class List : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Routes("/api/custom-activities");
        Verbs(Http.POST);
    }

    public override async Task<Response> ExecuteAsync(CancellationToken ct)
    {
        return new Response();
    }
}