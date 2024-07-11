using Elsa.Caching.Distributed.Contracts;
using Elsa.Caching.Distributed.MassTransit.Messages;
using JetBrains.Annotations;
using MassTransit;

namespace Elsa.Caching.Distributed.MassTransit.Consumers;

/// <summary>
/// Consumes <see cref="TriggerChangeTokenSignal"/> messages and triggers the change token signal.
/// </summary>
[UsedImplicitly]
public class TriggerChangeTokenSignalConsumer(IChangeTokenSignalInvoker changeTokenSignalInvoker) : IConsumer<TriggerChangeTokenSignal>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<TriggerChangeTokenSignal> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        await changeTokenSignalInvoker.InvokeAsync(message.Key, cancellationToken);
    }
}