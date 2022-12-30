using Elsa.Common.Services;
using Elsa.Mediator.Services;

namespace Elsa.Mediator.Implementations;

public class InProcessTransport<T> : ITransport<T> where T : INotification
{
    private readonly IBackgroundEventPublisher _publisher;

    public InProcessTransport(IBackgroundEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task SendAsync(T message, CancellationToken cancellationToken)
    {
        await _publisher.PublishAsync(message, cancellationToken);
    }
}