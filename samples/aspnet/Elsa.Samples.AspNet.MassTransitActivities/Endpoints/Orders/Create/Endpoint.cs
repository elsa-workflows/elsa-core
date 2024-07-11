using Elsa.Samples.AspNet.MassTransitActivities.Messages;
using FastEndpoints;
using MassTransit;

namespace Elsa.Samples.AspNet.MassTransitActivities.Endpoints.Orders.Create;

public class Create : EndpointWithoutRequest
{
    private readonly IBus _bus;

    public Create(IBus bus)
    {
        _bus = bus;
    }
    
    public override void Configure()
    {
        Post("orders");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        await _bus.Publish<OrderCreated>(new
        {
            Id = Guid.NewGuid().ToString("N"),
            CustomerId = "1",
            Product = "Pizza",
            Quantity = 5,
            Total = 62.5
        }, cancellationToken);
    }
}