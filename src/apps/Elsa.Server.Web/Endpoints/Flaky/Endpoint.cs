using FastEndpoints;

namespace Elsa.Server.Web.Endpoints.Flaky;

public class FlakyResponse
{
    public string Message { get; set; }
}

public class FlakyEndpoint(ILogger<FlakyEndpoint> logger) : EndpointWithoutRequest<FlakyResponse>
{
    private static int _count;

    public override void Configure()
    {
        Get("/flaky");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var call = Interlocked.Increment(ref _count);

        switch (call)
        {
            case 1:
                logger.LogInformation("Too Many Requests");
                await SendAsync(new()
                {
                    Message = "Too Many Requests"
                }, StatusCodes.Status429TooManyRequests, ct);
                break;
            case 2:
                logger.LogInformation("Service Unavailable");
                await SendAsync(new()
                {
                    Message = "Service Unavailable"
                }, StatusCodes.Status503ServiceUnavailable, ct);
                break;
            default:
                logger.LogInformation("OK");
                await SendAsync(new()
                {
                    Message = "OK"
                }, StatusCodes.Status200OK, ct);
                Interlocked.Exchange(ref _count, 0);
                break;
        }
    }
}