using Elsa.Caching;
using Elsa.Caching.Distributed.Contracts;
using Elsa.Testing.Shared;
using Hangfire.Annotations;
using Microsoft.Extensions.Primitives;

namespace Elsa.Workflows.ComponentTests.Helpers.Decorators;

/// Provides a decorator for the `IChangeTokenSignaler` interface that triggers change token events.
/// This class is used to raise change token signal events and delegate the actual work to the decorated service.
[UsedImplicitly]
public class EventPublishingChangeTokenSignaler(IChangeTokenSignaler decoratedService, ITriggerChangeTokenSignalEvents triggerChangeTokenSignalEvents) : IChangeTokenSignaler
{
    public IChangeToken GetToken(string key)
    {
        return decoratedService.GetToken(key);
    }

    public ValueTask TriggerTokenAsync(string key, CancellationToken cancellationToken = default)
    {
        triggerChangeTokenSignalEvents.RaiseChangeTokenSignalTriggered(new TriggerChangeTokenSignalEventArgs(key));
        return decoratedService.TriggerTokenAsync(key, cancellationToken);
    }
}