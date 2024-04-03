using Elsa.Caching.Contracts;
using Elsa.Caching.Distributed.MassTransit.Messages;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.Caching.Distributed.MassTransit.Consumers;

/// <summary>
/// Consumes <see cref="TriggerChangeTokenSignal"/> messages and triggers the change token signal.
/// </summary>
[UsedImplicitly]
public class TriggerChangeTokenSignalConsumer(IDistributedChangeTokenSignaler changeTokenSignaler) : IConsumer<TriggerChangeTokenSignal>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<TriggerChangeTokenSignal> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        await changeTokenSignaler.TriggerTokenLocalAsync(message.Key, cancellationToken);
    }
}