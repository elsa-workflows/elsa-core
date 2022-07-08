using FastEndpoints;

namespace Elsa.CustomActivities.Endpoints.ActivityDefinitions.Create;

public class Create : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Routes("/api/custom-activities");
        Verbs(Http.POST);
    }

    public override async Task<Response> ExecuteAsync(Request req, CancellationToken ct)
    {
        return new Response();
    }
}