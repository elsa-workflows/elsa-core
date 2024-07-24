using Elsa.Caching;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Services;
using Hangfire.Annotations;
using Microsoft.Extensions.Primitives;

namespace Elsa.Workflows.ComponentTests.Helpers;

/// Provides a decorator for the `IChangeTokenSignaler` interface that triggers change token events.
/// This class is used to raise change token signal events and delegate the actual work to the decorated service.
[UsedImplicitly]
public class EventPublishingChangeTokenSignaler(IChangeTokenSignaler decoratedService, TriggerChangeTokenSignalEvents triggerChangeTokenSignalEvents) : IChangeTokenSignaler
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