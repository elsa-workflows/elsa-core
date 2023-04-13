using FastEndpoints;

namespace Elsa.WorkflowServer.Web.Endpoints;

public class SlowEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/slow");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), ct);
        await SendOkAsync(ct);
    }
}