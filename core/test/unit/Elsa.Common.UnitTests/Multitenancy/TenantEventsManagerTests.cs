using Elsa.Common.Multitenancy;
using Microsoft.Extensions.Logging;
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
    public async Task AllTenantsActivatedAsync_WhenCancelledHandlerThrows_PropagatesWithoutErrorLog()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var failing = Substitute.For<IAllTenantsActivatedEvent>();
        failing.AllTenantsActivatedAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new OperationCanceledException(cts.Token)));
        var logger = Substitute.For<ILogger<TenantEventsManager>>();
        var manager = new TenantEventsManager([], [], [], [failing], logger);

        await Assert.ThrowsAsync<OperationCanceledException>(() => manager.AllTenantsActivatedAsync(cts.Token));

        Assert.DoesNotContain(logger.ReceivedCalls(), x => x.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)x.GetArguments()[0]! == LogLevel.Error);
    }

    [Fact]
    public async Task AllTenantsActivatedAsync_WhenHandlerThrows_LogsErrorAndContinuesWithRemainingHandlers()
    {
        var failing = Substitute.For<IAllTenantsActivatedEvent>();
        failing.AllTenantsActivatedAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("Recovery failed")));
        var next = Substitute.For<IAllTenantsActivatedEvent>();
        var logger = Substitute.For<ILogger<TenantEventsManager>>();
        var manager = new TenantEventsManager([], [], [], [failing, next], logger);

        await manager.AllTenantsActivatedAsync(CancellationToken.None);

        await next.Received(1).AllTenantsActivatedAsync(Arg.Any<CancellationToken>());
        Assert.Contains(logger.ReceivedCalls(), x => x.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)x.GetArguments()[0]! == LogLevel.Error);
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

    [Fact]
    public async Task TenantDeactivatedAsync_WhenCancelledHandlerThrows_LogsNoError()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var failing = Substitute.For<ITenantDeactivatedEvent>();
        failing.TenantDeactivatedAsync(Arg.Any<TenantDeactivatedEventArgs>()).Returns(Task.FromException(new OperationCanceledException(cts.Token)));
        var logger = Substitute.For<ILogger<TenantEventsManager>>();
        var manager = new TenantEventsManager([], [failing], [], logger);

        await manager.TenantDeactivatedAsync(new(Tenant.Default, null!, cts.Token));

        Assert.DoesNotContain(logger.ReceivedCalls(), x => x.GetMethodInfo().Name == nameof(ILogger.Log) && (LogLevel)x.GetArguments()[0]! == LogLevel.Error);
    }
}
