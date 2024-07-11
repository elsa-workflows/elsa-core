using Elsa.Samples.AspNet.MassTransitWorkflow.Messages;
using FastEndpoints;
using MassTransit;

namespace Elsa.Samples.AspNet.MassTransitWorkflow.Endpoints.Messages.Create;

public class Create : EndpointWithoutRequest
{
    private readonly IBus _bus;

    public Create(IBus bus)
    {
        _bus = bus;
    }
    
    public override void Configure()
    {
        Post("messages");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        await _bus.Publish<Message>(new Message
        {
            Content = "Hello World from the bus."
        }, cancellationToken);
    }
}