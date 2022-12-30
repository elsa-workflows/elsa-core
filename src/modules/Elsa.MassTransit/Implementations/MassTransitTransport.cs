using Elsa.Common.Services;
using MassTransit;

namespace Elsa.MassTransit.Implementations;

public class MassTransitTransport<T> : ITransport<T>
{
    private readonly IBus _bus;

    public MassTransitTransport(IBus bus)
    {
        _bus = bus;
    }
    
    public async Task SendAsync(T message, CancellationToken cancellationToken)
    {
        await _bus.Publish(message, cancellationToken);
    }
}