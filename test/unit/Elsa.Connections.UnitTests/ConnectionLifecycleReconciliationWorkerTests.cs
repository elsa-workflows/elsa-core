using System.Collections;
using System.Reflection;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Features;
using Elsa.Connections.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Connections.UnitTests;

public sealed class ConnectionLifecycleReconciliationWorkerTests
{
    [Fact]
    public async Task RoutesEveryCandidateKindWithConfiguredConcurrencyBound()
    {
        var tenantAccessor = new DefaultTenantAccessor();
        var dueStore = Substitute.For<IConnectionDueCandidateStore>();
        var recovery = Substitute.For<IConnectionLifecycleRecoveryService>();
        var scope = new ConnectionLifecycleScope("trusted-tenant", "production");
        var kinds = Enum.GetValues<ConnectionDueCandidateKind>();
        var candidates = kinds.Select((kind, index) => new ConnectionDueCandidate(scope.TenantId, scope.EnvironmentId,
            $"connection-{index}", kind, $"candidate-{index}", DateTimeOffset.UtcNow)).ToArray();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;
        var maxActive = 0;
        var count = 0;

        async Task RecordDispatchAsync()
        {
            var nowActive = Interlocked.Increment(ref active);
            UpdateMax(ref maxActive, nowActive);
            await Task.Delay(20);
            Interlocked.Decrement(ref active);
            if (Interlocked.Increment(ref count) == candidates.Length)
                completed.TrySetResult();
        }

        dueStore.FindDueCandidatesAsync(scope.TenantId, scope.EnvironmentId, Arg.Any<DateTimeOffset>(), candidates.Length, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ConnectionDueCandidatePage(candidates, null)));
        recovery.RefreshAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async call => { await RecordDispatchAsync(); return new ConnectionLifecycleResult(true, null, 1); });
        recovery.ReconcileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async call => { await RecordDispatchAsync(); return new ConnectionLifecycleResult(true, null, 1); });
        recovery.CleanupGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async call => { await RecordDispatchAsync(); return new ConnectionLifecycleResult(true, null, 1); });
        recovery.ReconcileOffboardingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async call => { await RecordDispatchAsync(); return new ConnectionOffboardingOperationResult(true, null, null, null, 1); });

        var services = new ServiceCollection();
        services.AddSingleton<ITenantAccessor>(tenantAccessor);
        services.AddSingleton(dueStore);
        services.AddSingleton<IConnectionLifecycleRecoveryService>(recovery);
        services.AddSingleton<IConnectionLifecycleScopeProvider>(new SingleScopeProvider(scope));
        await using var provider = services.BuildServiceProvider();
        using var worker = new ConnectionLifecycleReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ConnectionLifecycleReconciliationOptions
            {
                BatchSize = candidates.Length,
                MaxConcurrency = 2,
                Interval = TimeSpan.FromSeconds(30),
                RetryBackoff = TimeSpan.FromSeconds(1),
                MaxRetryBackoff = TimeSpan.FromSeconds(5)
            }),
            TimeProvider.System,
            NullLogger<ConnectionLifecycleReconciliationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }

        Assert.Equal(candidates.Length, count);
        Assert.InRange(maxActive, 1, 2);
        await recovery.Received(1).RefreshAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await recovery.Received(2).ReconcileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await recovery.Received(1).CleanupGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await recovery.Received(1).ReconcileOffboardingAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchesDueWorkInTheTrustedAmbientTenantScope()
    {
        var tenantAccessor = new DefaultTenantAccessor();
        var dueStore = Substitute.For<IConnectionDueCandidateStore>();
        var recovery = Substitute.For<IConnectionLifecycleRecoveryService>();
        var dispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scope = new ConnectionLifecycleScope("trusted-tenant", "production");
        var candidate = new ConnectionDueCandidate(scope.TenantId, scope.EnvironmentId, "connection-a",
            ConnectionDueCandidateKind.Offboarding, "operation-a", DateTimeOffset.UtcNow);

        dueStore.FindDueCandidatesAsync(scope.TenantId, scope.EnvironmentId, Arg.Any<DateTimeOffset>(), 10, null, Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                Assert.Equal(scope.TenantId, tenantAccessor.Tenant?.Id);
                return Task.FromResult(new ConnectionDueCandidatePage([candidate], null));
            });
        recovery.ReconcileOffboardingAsync(scope.TenantId, scope.EnvironmentId, candidate.ConnectionId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                Assert.Equal(scope.TenantId, tenantAccessor.Tenant?.Id);
                dispatched.TrySetResult();
                return Task.FromResult(new ConnectionOffboardingOperationResult(true, null, "operation-a", ConnectionOffboardingOperationStatus.Completed, 2));
            });

        var services = new ServiceCollection();
        services.AddSingleton<ITenantAccessor>(tenantAccessor);
        services.AddSingleton(dueStore);
        services.AddSingleton<IConnectionLifecycleRecoveryService>(recovery);
        services.AddSingleton<IConnectionLifecycleScopeProvider>(new SingleScopeProvider(scope));
        await using var provider = services.BuildServiceProvider();
        var options = Options.Create(new ConnectionLifecycleReconciliationOptions
        {
            BatchSize = 10,
            Interval = TimeSpan.FromSeconds(30),
            RetryBackoff = TimeSpan.FromSeconds(1),
            MaxRetryBackoff = TimeSpan.FromSeconds(5)
        });
        using var worker = new ConnectionLifecycleReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(), options, TimeProvider.System,
            NullLogger<ConnectionLifecycleReconciliationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }

        await dueStore.Received(1).FindDueCandidatesAsync(scope.TenantId, scope.EnvironmentId,
            Arg.Any<DateTimeOffset>(), 10, null, Arg.Any<CancellationToken>());
        await recovery.Received(1).ReconcileOffboardingAsync(scope.TenantId, scope.EnvironmentId,
            candidate.ConnectionId, Arg.Any<CancellationToken>());
        Assert.Null(tenantAccessor.Tenant);
    }

    [Fact]
    public async Task IgnoresCandidatesReturnedOutsideTheTrustedScope()
    {
        var dueStore = Substitute.For<IConnectionDueCandidateStore>();
        var recovery = Substitute.For<IConnectionLifecycleRecoveryService>();
        var scope = new ConnectionLifecycleScope("trusted-tenant", "production");
        var otherScopeCandidate = new ConnectionDueCandidate("other-tenant", scope.EnvironmentId, "connection-a",
            ConnectionDueCandidateKind.RecoveryRequired, "operation-a", DateTimeOffset.MinValue);
        dueStore.FindDueCandidatesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), 10, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ConnectionDueCandidatePage([otherScopeCandidate], null)));

        var services = new ServiceCollection();
        services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        services.AddSingleton(dueStore);
        services.AddSingleton<IConnectionLifecycleRecoveryService>(recovery);
        services.AddSingleton<IConnectionLifecycleScopeProvider>(new SingleScopeProvider(scope));
        await using var provider = services.BuildServiceProvider();
        using var worker = new ConnectionLifecycleReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ConnectionLifecycleReconciliationOptions
            {
                BatchSize = 10,
                Interval = TimeSpan.FromSeconds(30),
                RetryBackoff = TimeSpan.FromSeconds(1),
                MaxRetryBackoff = TimeSpan.FromSeconds(5)
            }),
            TimeProvider.System,
            NullLogger<ConnectionLifecycleReconciliationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await worker.StopAsync(CancellationToken.None);

        await recovery.DidNotReceiveWithAnyArgs().ReconcileAsync(default!, default!, default!, default);
        await recovery.DidNotReceiveWithAnyArgs().RefreshAsync(default!, default!, default!, default);
        await recovery.DidNotReceiveWithAnyArgs().CleanupGenerationAsync(default!, default!, default!, default!, default);
        await recovery.DidNotReceiveWithAnyArgs().ReconcileOffboardingAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task RepeatedScanWraparoundKeepsScopeCursorStateBounded()
    {
        var dueStore = Substitute.For<IConnectionDueCandidateStore>();
        var recovery = Substitute.For<IConnectionLifecycleRecoveryService>();
        var scope = new ConnectionLifecycleScope("trusted-tenant", "production");
        var candidate = new ConnectionDueCandidate(scope.TenantId, scope.EnvironmentId, "connection-a",
            ConnectionDueCandidateKind.RecoveryRequired, "operation-a", DateTimeOffset.MinValue);
        var queryCount = 0;
        var queriedEnough = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dueStore.FindDueCandidatesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(), 10, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var count = Interlocked.Increment(ref queryCount);
                if (count >= 1_100)
                    queriedEnough.TrySetResult();
                var cursor = call.ArgAt<string?>(4);
                return cursor is null
                    ? Task.FromResult(new ConnectionDueCandidatePage([candidate], "next-page"))
                    : Task.FromResult(new ConnectionDueCandidatePage([], null));
            });
        recovery.ReconcileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ConnectionLifecycleResult(true, null, 1)));

        var services = new ServiceCollection();
        services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        services.AddSingleton(dueStore);
        services.AddSingleton<IConnectionLifecycleRecoveryService>(recovery);
        services.AddSingleton<IConnectionLifecycleScopeProvider>(new SingleScopeProvider(scope));
        await using var provider = services.BuildServiceProvider();
        using var worker = new ConnectionLifecycleReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ConnectionLifecycleReconciliationOptions
            {
                BatchSize = 10,
                MaxConcurrency = 1,
                Interval = TimeSpan.FromMilliseconds(1),
                RetryBackoff = TimeSpan.FromSeconds(1),
                MaxRetryBackoff = TimeSpan.FromSeconds(2)
            }),
            TimeProvider.System,
            NullLogger<ConnectionLifecycleReconciliationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        try
        {
            await queriedEnough.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }

        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var cursorCount = Assert.IsAssignableFrom<IDictionary>(typeof(ConnectionLifecycleReconciliationWorker)
            .GetField("_cursors", flags)!.GetValue(worker)).Count;
        var scopeOrderCount = Assert.IsAssignableFrom<ICollection>(typeof(ConnectionLifecycleReconciliationWorker)
            .GetField("_scopeOrder", flags)!.GetValue(worker)).Count;
        Assert.True(queryCount >= 1_100);
        Assert.Equal(1, cursorCount);
        Assert.Equal(1, scopeOrderCount);
    }

    [Fact]
    public async Task DisabledWorkerDoesNotResolveScopeOrQueryDueCandidates()
    {
        var dueStore = Substitute.For<IConnectionDueCandidateStore>();
        var scopeProvider = Substitute.For<IConnectionLifecycleScopeProvider>();
        var services = new ServiceCollection();
        services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        services.AddSingleton(dueStore);
        services.AddSingleton(scopeProvider);
        await using var provider = services.BuildServiceProvider();
        using var worker = new ConnectionLifecycleReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ConnectionLifecycleReconciliationOptions
            {
                Enabled = false,
                Interval = TimeSpan.FromMilliseconds(10),
                RetryBackoff = TimeSpan.FromSeconds(1),
                MaxRetryBackoff = TimeSpan.FromSeconds(2)
            }),
            TimeProvider.System,
            NullLogger<ConnectionLifecycleReconciliationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        await scopeProvider.DidNotReceiveWithAnyArgs().GetNextScopeAsync(default);
        await dueStore.DidNotReceiveWithAnyArgs().FindDueCandidatesAsync(default!, default!, default, default, default, default);
    }

    private sealed class SingleScopeProvider(ConnectionLifecycleScope scope) : IConnectionLifecycleScopeProvider
    {
        public Task<ConnectionLifecycleScope?> GetNextScopeAsync(CancellationToken cancellationToken = default) => Task.FromResult<ConnectionLifecycleScope?>(scope);
    }

    private static void UpdateMax(ref int target, int value)
    {
        var current = Volatile.Read(ref target);
        while (value > current)
        {
            var previous = Interlocked.CompareExchange(ref target, value, current);
            if (previous == current)
                return;
            current = previous;
        }
    }
}
