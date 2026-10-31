using Elsa.Common.Multitenancy;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Common.UnitTests.Multitenancy;

public class TenantEventsManagerTests
{
    [Fact]
    public async Task TenantActivatedAsync_WhenHandlerThrows_PropagatesFailure()
    {
        var failing = Substitute.For<ITenantActivatedEvent>();
        failing.TenantActivatedAsync(Arg.Any<TenantActivatedEventArgs>()).Returns(Task.FromException(new InvalidOperationException("Migration failed")));
        var manager = new TenantEventsManager([failing], [], [], NullLogger<TenantEventsManager>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.TenantActivatedAsync(new(Tenant.Default, null!, CancellationToken.None)));
    }

    [Fact]
    public async Task TenantDeactivatedAsync_WhenHandlerThrows_ContinuesWithRemainingHandlers()
    {
        var failing = Substitute.For<ITenantDeactivatedEvent>();
        failing.TenantDeactivatedAsync(Arg.Any<TenantDeactivatedEventArgs>()).Returns(Task.FromException(new InvalidOperationException()));
        var next = Substitute.For<ITenantDeactivatedEvent>();
        var manager = new TenantEventsManager([], [failing, next], [], NullLogger<TenantEventsManager>.Instance);

        await manager.TenantDeactivatedAsync(new(Tenant.Default, null!, CancellationToken.None));

        await next.Received(1).TenantDeactivatedAsync(Arg.Any<TenantDeactivatedEventArgs>());
    }
}
