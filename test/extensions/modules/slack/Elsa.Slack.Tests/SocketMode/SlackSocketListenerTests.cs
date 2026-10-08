using Elsa.Common.Multitenancy;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Transport;
using Elsa.Workflows.Admission;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Slack.Tests.SocketMode;

public sealed class SlackSocketListenerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedOrBlockedHostAttestationCannotOpenTransportOrWithdrawThroughAnUnreviewedStore(bool block)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = Substitute.For<IAdmissionStore>();
        var configuration = SocketModeTestData.Configuration(SocketModeTestData.Limits with
        {
            OperationTimeout = TimeSpan.FromMilliseconds(200), DrainTimeout = TimeSpan.FromMilliseconds(200)
        });
        var services = new ServiceCollection();
        services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        services.AddSingleton(store);
        services.AddScoped(_ => new ScopeProbe(disposed));
        services.AddScoped<SlackSocketModeHostValidator>(provider =>
        {
            _ = provider.GetRequiredService<ScopeProbe>();
            entered.TrySetResult();
            if (block)
            {
                released.Task.GetAwaiter().GetResult();
            }
            throw new InvalidOperationException("synthetic-private-validation-error");
        });
        services.AddScoped(_ => new SlackSocketSubscriptionWithdrawal(configuration, store));
        await using var provider = services.BuildServiceProvider();
        var health = new SlackSocketModeHealth(8, 2);
        await using var listener = new SlackSocketListener(configuration, provider.GetRequiredService<IServiceScopeFactory>(),
            SlackSocketTransportPolicy.Production, health, TimeProvider.System);
        try
        {
            await listener.StartAsync(CancellationToken.None);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (!block)
            {
                await disposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            await listener.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
            if (block)
            {
                Assert.False(disposed.Task.IsCompleted);
                Assert.Equal(SlackSocketModeHealthState.ReconciliationRequired, health.GetSnapshot().State);
                Assert.Equal(SlackSocketModeHealthReason.Drain, health.GetSnapshot().Reason);
            }
        }
        finally
        {
            released.TrySetResult();
            await disposed.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await listener.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
        }
        await store.DidNotReceive().FindSubscriptionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().WithdrawAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        // The failed validator occurs before resolving any URL opener or credential reader; neither is registered.
        Assert.Equal(SlackSocketModeHealthState.ReconciliationRequired, health.GetSnapshot().State);
        Assert.Equal(block ? SlackSocketModeHealthReason.Drain : SlackSocketModeHealthReason.Provisioning, health.GetSnapshot().Reason);
    }

    private sealed class ScopeProbe(TaskCompletionSource disposed) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            disposed.TrySetResult();
            return ValueTask.CompletedTask;
        }
    }
}
