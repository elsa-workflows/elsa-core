using Elsa.Abstractions;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Server.Web;

public class MyEndpoint : ElsaEndpointWithoutRequest
{
    private readonly IEventPublisher _eventPublisher;

    public MyEndpoint(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }
    
    public override void Configure()
    {
        Get("/my-event-workflow");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        Console.WriteLine("Publishing MyEvent");
        var results = await _eventPublisher.PublishAsync("MyEvent", cancellationToken: ct);
        Console.WriteLine($"Affected workflows: {results.Count}");
    }
}