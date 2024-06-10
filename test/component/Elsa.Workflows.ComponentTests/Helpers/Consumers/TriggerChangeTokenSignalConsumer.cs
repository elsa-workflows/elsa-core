using Elsa.Caching.Distributed.MassTransit.Messages;
using Elsa.Testing.Shared;
using Hangfire.Annotations;
using MassTransit;

namespace Elsa.Workflows.ComponentTests.Consumers;

[UsedImplicitly]
public class TriggerChangeTokenSignalConsumer(ITriggerChangeTokenSignalEvents triggerChangeTokenSignalEvents) : IConsumer<TriggerChangeTokenSignal>
{
    public Task Consume(ConsumeContext<TriggerChangeTokenSignal> context)
    {
        triggerChangeTokenSignalEvents.OnChangeTokenSignalTriggered(new TriggerChangeTokenSignalEventArgs(context.Message.Key));
        return Task.CompletedTask;
    }
}