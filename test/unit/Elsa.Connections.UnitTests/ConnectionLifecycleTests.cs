using System.Security.Claims;
using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Connections.Persistence.EFCore;
using Elsa.Connections.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Connections.Services;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Connections.Features;
using Elsa.Connections.Persistence.EFCore.Features;
using Elsa.Secrets.Features;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Secrets.Services;
using Elsa.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Tenants.Options;

namespace Elsa.Connections.UnitTests;

public sealed class ConnectionLifecycleTests
{
    private const string TenantId = "tenant-a";
    private const string EnvironmentId = "production";

    [Fact]
    public async Task DefaultAuthorizerDeniesClientSelectedSystemAndFeatureServiceHasNoKindParameter()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        module.Configure<ConnectionsFeature>();
        module.Apply();
        await using var provider = services.BuildServiceProvider();

        var forgedSystemPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user"), new Claim("elsa:identity-kind", "system")], "synthetic"));
        var allowed = await provider.GetRequiredService<IConnectionUseAuthorizer>().AuthorizeAsync(new ConnectionUseRequest(
            forgedSystemPrincipal, ConnectionUseKind.BackgroundSystem, TenantId, EnvironmentId, "connection", "manage:reconcile"));

        Assert.False(allowed);
        Assert.DoesNotContain(typeof(IConnectionLifecycleService).GetMethods(), method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(ConnectionUseKind)));
        Assert.DoesNotContain(typeof(IConnectionBackgroundUseService).GetMethods(), method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(ConnectionUseKind) || parameter.ParameterType == typeof(ClaimsPrincipal)));
    }

    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public async Task IndependentLiveWorkersRaceOnceThenFreshWorkerAfterDisposalDecryptsWithExternalKey(int keyLength)
    {
        await using var database = new TestDatabase();
        var databasePath = database.Path;
        var key = MakeKey(keyLength);
        var provider = new SyntheticCredentialProvider(block: true);
        await using var worker1 = await Worker.CreateAsync(databasePath, key, provider);
        var connectionId = await SeedAsync(worker1);
        await using var worker2 = await Worker.CreateAsync(databasePath, key, provider, migrate: false);

        using (var scope1 = worker1.Services.CreateScope())
        using (var scope2 = worker2.Services.CreateScope())
        {
            var firstRefresh = scope1.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
            await provider.Entered.WaitAsync(TimeSpan.FromSeconds(15));

            var concurrentRefresh = await scope2.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.False(concurrentRefresh.Succeeded);
            provider.Complete(new CredentialMaterial("access-rotated", "refresh-rotated", DateTimeOffset.UtcNow.AddHours(1)));

            var refreshed = await firstRefresh;
            Assert.True(refreshed.Succeeded);
        }

        await worker1.StopAsync();
        await worker2.StopAsync();
        Assert.Equal(1, provider.CallCount);
        Assert.True(provider.IsConsumed("refresh-initial"));
        Assert.False(provider.IsAvailable("refresh-initial"));
        Assert.True(provider.IsAvailable("refresh-rotated"));

        // The live worker containers are disposed above. A fresh container and manager now decrypt the committed
        // generation using the same key supplied by host configuration, exercising an actual process restart boundary.
        await using var restartedWorker = await Worker.CreateAsync(databasePath, key, provider, migrate: false);
        ConnectionAccessCredential access;
        using (var restartedScope = restartedWorker.Services.CreateScope())
            access = await restartedScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .ResolveForUseAsync(Principal(), TenantId, EnvironmentId, connectionId);
        await restartedWorker.StopAsync();

        Assert.Equal("access-rotated", access.AccessToken);
        Assert.DoesNotContain("refresh-rotated", JsonSerializer.Serialize(access));
        Assert.DoesNotContain("access-rotated", JsonSerializer.Serialize(access));
        Assert.Contains("Redacted", access.ToString());

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.RefreshAsync("synthetic-oauth", "account-test", "refresh-initial"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.RefreshAsync("synthetic-oauth", "account-test", "unknown-refresh-token"));
        Assert.Equal(1, provider.AcceptedCallCount);

        var wrongKeyWorker = await Worker.CreateAsync(databasePath, MakeKey(keyLength, mismatch: true), provider, migrate: false);
        await using (wrongKeyWorker)
        using (var wrongScope = wrongKeyWorker.Services.CreateScope())
        {
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => wrongScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .ResolveForUseAsync(Principal(), TenantId, EnvironmentId, connectionId));
            Assert.DoesNotContain("access-rotated", exception.ToString());
            Assert.DoesNotContain("refresh-rotated", exception.ToString());
        }

        var missingKeyWorker = await Worker.CreateAsync(databasePath, null, provider, migrate: false);
        await using (missingKeyWorker)
        using (var missingScope = missingKeyWorker.Services.CreateScope())
        {
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => missingScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .ResolveForUseAsync(Principal(), TenantId, EnvironmentId, connectionId));
        }
    }

    [Fact]
    public async Task DisconnectDuringProviderCallPreventsPublishAndRecoveryCannotReviveIt()
    {
        await using var database = new TestDatabase();
        var databasePath = database.Path;
        var key = MakeKey(32);
        var provider = new SyntheticCredentialProvider(block: true);
        await using var worker1 = await Worker.CreateAsync(databasePath, key, provider);
        var connectionId = await SeedAsync(worker1);
        await using var worker2 = await Worker.CreateAsync(databasePath, key, provider, migrate: false);

        using var refreshScope = worker1.Services.CreateScope();
        var refreshTask = refreshScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
            .RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
        await provider.Entered.WaitAsync(TimeSpan.FromSeconds(15));

        using (worker2.TenantAccessor.PushContext(TenantContext()))
        using (var disconnectScope = worker2.Services.CreateScope())
        {
            var store = disconnectScope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var claimed = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            Assert.NotNull(claimed);
            Assert.True(await store.TryDisconnectAsync(connectionId, TenantId, EnvironmentId, claimed.Revision));
        }

        provider.Complete(new CredentialMaterial("access-stale", "refresh-stale", DateTimeOffset.UtcNow.AddHours(1)));
        var result = await refreshTask;
        Assert.False(result.Succeeded);

        using (worker2.TenantAccessor.PushContext(TenantContext()))
        using (var reconcileScope = worker2.Services.CreateScope())
        {
            var store = reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var before = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            Assert.Equal(ConnectionStatus.Disconnected, before!.Status);
            Assert.NotNull(before.CurrentGenerationId);

            var reconciliation = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
                .ReconcileAsync(TenantId, EnvironmentId, connectionId);
            Assert.False(reconciliation.Succeeded);
            var after = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            Assert.Equal(ConnectionStatus.Disconnected, after!.Status);
            Assert.Equal(before.CurrentGenerationId, after.CurrentGenerationId);
        }

        using var useScope = worker2.Services.CreateScope();
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => useScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
            .ResolveForUseAsync(Principal(), TenantId, EnvironmentId, connectionId));
    }

    [Fact]
    public async Task LostStageWriteCanRecoverPlannedGenerationWithoutReplayingProviderCall()
    {
        await using var database = new TestDatabase();
        var databasePath = database.Path;
        var key = MakeKey(32);
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker1 = await Worker.CreateAsync(databasePath, key, provider, failBeforeStageWrite: true);
        var connectionId = await SeedAsync(worker1);
        worker1.StageWriteFault!.FailNextStageWrite();
        using (var refreshScope = worker1.Services.CreateScope())
        {
            var result = await refreshScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.False(result.Succeeded);
        }

        Assert.Equal(1, provider.CallCount);
        Assert.Equal(1, provider.AcceptedCallCount);
        Assert.True(provider.IsConsumed("refresh-initial"));
        Assert.False(provider.IsAvailable("refresh-initial"));
        Assert.True(provider.IsAvailable("refresh-rotated"));
        await worker1.StopAsync();

        await using var restartedWorker = await Worker.CreateAsync(databasePath, key, provider, migrate: false);
        using (var reconcileScope = restartedWorker.Services.CreateScope())
        using (restartedWorker.TenantAccessor.PushContext(TenantContext()))
        {
            var lifecycle = reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>();
            var recovered = await lifecycle.ReconcileAsync(TenantId, EnvironmentId, connectionId);
            Assert.True(recovered.Succeeded);

            var row = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>().FindAsync(connectionId, TenantId, EnvironmentId);
            Assert.Equal(ConnectionStatus.Active, row!.Status);
            Assert.Equal(CredentialOperationStatus.Completed, row.OperationStatus);
            Assert.Equal(row.PlannedGenerationId, row.CurrentGenerationId);
        }

        using (var nextRefreshScope = restartedWorker.Services.CreateScope())
        {
            var nextRefresh = await nextRefreshScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.True(nextRefresh.Succeeded);
        }

        Assert.Equal(2, provider.CallCount);
        Assert.Equal(2, provider.AcceptedCallCount);
        Assert.True(provider.IsConsumed("refresh-rotated"));
        Assert.True(provider.IsAvailable("refresh-rotated-2"));
    }

    [Fact]
    public async Task RestartWithProviderCallStartedMarksRecoveryWithoutProviderReplay()
    {
        await using var database = new TestDatabase();
        var databasePath = database.Path;
        var clock = new TestTimeProvider(DateTimeOffset.UtcNow);
        var key = MakeKey(32);
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker1 = await Worker.CreateAsync(databasePath, key, provider, timeProvider: clock);
        var connectionId = await SeedAsync(worker1);

        using (worker1.TenantAccessor.PushContext(TenantContext()))
        using (var scope = worker1.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var current = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            var operationId = "crash-after-provider-call-start";
            var claimed = await store.TryClaimRefreshAsync(connectionId, TenantId, EnvironmentId, current!.Revision, operationId, clock.GetUtcNow() + TimeSpan.FromMinutes(2));
            Assert.NotNull(claimed);
            Assert.True(await store.TryStartProviderCallAsync(connectionId, TenantId, EnvironmentId, claimed.OperationExpectedRevision, operationId, claimed.OperationFence));
        }

        await worker1.StopAsync();
        clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));

        await using var restartedWorker = await Worker.CreateAsync(databasePath, key, provider, migrate: false, timeProvider: clock);
        using var restartedScope = restartedWorker.Services.CreateScope();
        var reconciliation = await restartedScope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
            .ReconcileAsync(TenantId, EnvironmentId, connectionId);
        Assert.False(reconciliation.Succeeded);
        Assert.Equal(0, provider.CallCount);

        using (restartedWorker.TenantAccessor.PushContext(TenantContext()))
        {
            var row = await restartedScope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>().FindAsync(connectionId, TenantId, EnvironmentId);
            Assert.Equal(ConnectionStatus.RecoveryRequired, row!.Status);
            Assert.Equal(CredentialOperationStatus.RecoveryRequired, row.OperationStatus);
        }
    }

    [Fact]
    public async Task ReconcilerWaitsForLiveRefreshLeaseThenRecoversWithoutReplay()
    {
        await using var database = new TestDatabase();
        var clock = new TestTimeProvider(DateTimeOffset.UtcNow);
        var key = MakeKey(32);
        var provider = new SyntheticCredentialProvider(block: true);
        await using var worker1 = await Worker.CreateAsync(database.Path, key, provider, timeProvider: clock);
        var connectionId = await SeedAsync(worker1);
        await using var worker2 = await Worker.CreateAsync(database.Path, key, provider, migrate: false, timeProvider: clock);

        using var refreshScope = worker1.Services.CreateScope();
        var refreshTask = refreshScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
            .RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
        await provider.Entered.WaitAsync(TimeSpan.FromSeconds(15));

        using (var reconcileScope = worker2.Services.CreateScope())
        {
            var result = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
                .ReconcileAsync(TenantId, EnvironmentId, connectionId);
            Assert.False(result.Succeeded);
            Assert.Equal("operation_in_progress", result.SafeErrorCode);
            using (worker2.TenantAccessor.PushContext(TenantContext()))
            {
                var connection = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>()
                    .FindAsync(connectionId, TenantId, EnvironmentId);
                Assert.Equal(ConnectionStatus.Active, connection!.Status);
                Assert.Equal(CredentialOperationStatus.ProviderCallStarted, connection.OperationStatus);
                Assert.Equal(clock.GetUtcNow() + TimeSpan.FromMinutes(2), connection.OperationLeaseExpiresAt);
            }
        }

        Assert.Equal(1, provider.CallCount);
        Assert.Equal(1, provider.AcceptedCallCount);
        clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));

        using (var reconcileScope = worker2.Services.CreateScope())
        {
            var result = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
                .ReconcileAsync(TenantId, EnvironmentId, connectionId);
            Assert.False(result.Succeeded);
            Assert.Equal("refresh_outcome_unknown", result.SafeErrorCode);
            using (worker2.TenantAccessor.PushContext(TenantContext()))
            {
                var connection = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>()
                    .FindAsync(connectionId, TenantId, EnvironmentId);
                Assert.Equal(ConnectionStatus.RecoveryRequired, connection!.Status);
                Assert.Equal(CredentialOperationStatus.RecoveryRequired, connection.OperationStatus);
                Assert.Null(connection.OperationLeaseExpiresAt);
            }
        }

        provider.Complete(new CredentialMaterial("access-after-recovery", "refresh-after-recovery", clock.GetUtcNow().AddHours(1)));
        var ownerResult = await refreshTask;
        Assert.False(ownerResult.Succeeded);
        Assert.Equal(1, provider.CallCount);
        Assert.Equal(1, provider.AcceptedCallCount);

        using (var reconcileScope = worker2.Services.CreateScope())
        {
            var recovered = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
                .ReconcileAsync(TenantId, EnvironmentId, connectionId);
            Assert.True(recovered.Succeeded);
        }

        Assert.Equal(1, provider.CallCount);
        Assert.Equal(1, provider.AcceptedCallCount);
        Assert.True(provider.IsConsumed("refresh-initial"));
    }

    [Fact]
    public async Task InitialConnectLeaseProtectsLiveOperationAndRecoversAfterExpiry()
    {
        await using var database = new TestDatabase();
        var clock = new TestTimeProvider(DateTimeOffset.UtcNow);
        var gate = new ConnectGenerationGate();
        var provider = new SyntheticCredentialProvider(block: false);
        var key = MakeKey(32);
        await using var worker1 = await Worker.CreateAsync(database.Path, key, provider, timeProvider: clock, connectGenerationGate: gate);
        await using var worker2 = await Worker.CreateAsync(database.Path, key, provider, migrate: false, timeProvider: clock);

        using var connectScope = worker1.Services.CreateScope();
        var connectTask = connectScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>().ConnectAsync(
            Principal(),
            new ConnectConnectionRequest(TenantId, EnvironmentId, "synthetic-oauth", "account-test",
                new CredentialMaterial("access-initial", "refresh-initial", clock.GetUtcNow().AddHours(1))));
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(15));

        IntegrationConnection pending;
        using (worker1.TenantAccessor.PushContext(TenantContext()))
        {
            await using var context = await worker1.Services.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync();
            pending = await context.Connections.SingleAsync();
        }
        Assert.Equal(CredentialOperationStatus.CredentialReceived, pending.OperationStatus);
        Assert.Equal(clock.GetUtcNow() + TimeSpan.FromMinutes(2), pending.OperationLeaseExpiresAt);

        using (var reconcileScope = worker2.Services.CreateScope())
        {
            var early = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
                .ReconcileAsync(TenantId, EnvironmentId, pending.Id);
            Assert.False(early.Succeeded);
            Assert.Equal("operation_in_progress", early.SafeErrorCode);
        }

        clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));
        using (var reconcileScope = worker2.Services.CreateScope())
        {
            var expired = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
                .ReconcileAsync(TenantId, EnvironmentId, pending.Id);
            Assert.False(expired.Succeeded);
            Assert.Equal("refresh_outcome_unknown", expired.SafeErrorCode);
        }

        gate.Release();
        var connectResult = await connectTask;
        Assert.False(connectResult.Succeeded);
        Assert.Equal(pending.Id, connectResult.ConnectionId);

        using (var reconcileScope = worker2.Services.CreateScope())
        {
            var recovered = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
                .ReconcileAsync(TenantId, EnvironmentId, pending.Id);
            Assert.True(recovered.Succeeded);
        }

        using var useScope = worker2.Services.CreateScope();
        var access = await useScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
            .ResolveForUseAsync(Principal(), TenantId, EnvironmentId, pending.Id);
        Assert.Equal("access-initial", access.AccessToken);
    }

    [Fact]
    public async Task SensitiveProviderCancellationIsReplacedWithSanitizedCancellation()
    {
        await using var database = new TestDatabase();
        var databasePath = database.Path;
        var key = MakeKey(32);
        var provider = new SyntheticCredentialProvider(block: true, throwSensitiveCancellation: true);
        await using var worker = await Worker.CreateAsync(databasePath, key, provider);
        var connectionId = await SeedAsync(worker);
        using var scope = worker.Services.CreateScope();
        using var cancellation = new CancellationTokenSource();
        var refresh = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
            .RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId, cancellation.Token);

        await provider.Entered.WaitAsync(TimeSpan.FromSeconds(15));
        cancellation.Cancel();
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh);

        Assert.DoesNotContain("refresh-rotated", exception.ToString());
        Assert.DoesNotContain("provider-response-body", exception.ToString());
        Assert.Contains("outcome is unknown", exception.Message);
        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public async Task UsePermissionDoesNotGrantCredentialManagement()
    {
        await using var database = new TestDatabase();
        var databasePath = database.Path;
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker = await Worker.CreateAsync(databasePath, MakeKey(32), provider);
        var connectionId = await SeedAsync(worker);
        using var scope = worker.Services.CreateScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();

        var resolved = await lifecycle.ResolveForUseAsync(Principal(canManage: false), TenantId, EnvironmentId, connectionId);
        Assert.Equal("access-initial", resolved.AccessToken);
        var refresh = await lifecycle.RefreshAsync(Principal(canManage: false), TenantId, EnvironmentId, connectionId);
        Assert.False(refresh.Succeeded);
        Assert.Equal("connection_unavailable", refresh.SafeErrorCode);
        Assert.Equal(0, provider.CallCount);

        var backgroundUse = scope.ServiceProvider.GetRequiredService<IConnectionBackgroundUseService>();
        var backgroundAccess = await backgroundUse.ResolveForUseAsync(TenantId, EnvironmentId, connectionId);
        Assert.Equal("access-initial", backgroundAccess.AccessToken);
        var authorizer = (AllowUseAuthorizer)worker.Services.GetRequiredService<IConnectionUseAuthorizer>();
        var backgroundRequest = authorizer.LastBackgroundRequest!;
        Assert.Equal(ConnectionUseKind.BackgroundSystem, backgroundRequest.Kind);
        Assert.Equal("Elsa.Connections.Server", backgroundRequest.Principal.Identity!.AuthenticationType);
        Assert.Equal("use", backgroundRequest.Purpose);
        Assert.Equal(TenantId, backgroundRequest.TenantId);
        Assert.Equal(EnvironmentId, backgroundRequest.EnvironmentId);
        Assert.Equal(connectionId, backgroundRequest.ConnectionId);

        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => backgroundUse.ResolveForUseAsync("tenant-b", EnvironmentId, connectionId));
        Assert.Equal("tenant-b", authorizer.LastBackgroundRequest!.TenantId);
    }

    private static async Task<string> SeedAsync(Worker worker)
    {
        var provider = (SyntheticCredentialProvider)worker.Services.GetRequiredService<IConnectionCredentialProvider>();
        provider.IssueRefreshToken("refresh-initial");
        using var scope = worker.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>().ConnectAsync(
            Principal(),
            new ConnectConnectionRequest(TenantId, EnvironmentId, "synthetic-oauth", "account-test",
                new CredentialMaterial("access-initial", "refresh-initial", DateTimeOffset.UtcNow.AddHours(1))));
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ConnectionId);
        return result.ConnectionId;
    }

    private static ClaimsPrincipal Principal(bool canManage = true, bool canUse = true)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user") };
        if (canUse)
            claims.Add(new Claim("permission", "connections.use"));
        if (canManage)
            claims.Add(new Claim("permission", "connections.manage"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "synthetic"));
    }
    private static Tenant TenantContext() => new() { Id = TenantId, Name = TenantId };
    private static byte[] MakeKey(int length, bool mismatch = false) => Enumerable.Range(1, length).Select(x => (byte)(x + (mismatch ? 1 : 0))).ToArray();

    private sealed class SyntheticCredentialProvider(bool block, bool throwSensitiveCancellation = false) : IConnectionCredentialProvider
    {
        private readonly TaskCompletionSource<CredentialMaterial> _response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _gate = new();
        private readonly HashSet<string> _availableTokens = new(StringComparer.Ordinal);
        private readonly HashSet<string> _consumedTokens = new(StringComparer.Ordinal);
        private int _callCount;
        private int _acceptedCallCount;

        public int CallCount => Volatile.Read(ref _callCount);
        public int AcceptedCallCount => Volatile.Read(ref _acceptedCallCount);
        public Task Entered => _entered.Task;
        public void IssueRefreshToken(string token)
        {
            lock (_gate)
            {
                if (!_availableTokens.Add(token))
                    throw new InvalidOperationException("synthetic_duplicate_refresh_token");
            }
        }

        public bool IsAvailable(string token)
        {
            lock (_gate)
                return _availableTokens.Contains(token);
        }

        public bool IsConsumed(string token)
        {
            lock (_gate)
                return _consumedTokens.Contains(token);
        }

        public void Complete(CredentialMaterial material)
        {
            IssueRefreshToken(material.RefreshToken);
            _response.TrySetResult(material);
        }

        public async Task<CredentialMaterial> RefreshAsync(string providerId, string accountId, string refreshToken, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            lock (_gate)
            {
                if (!_availableTokens.Remove(refreshToken))
                    throw new InvalidOperationException("synthetic_invalid_grant");

                _consumedTokens.Add(refreshToken);
                Interlocked.Increment(ref _acceptedCallCount);
            }

            _entered.TrySetResult();
            if (block)
            {
                try
                {
                    return await _response.Task.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (throwSensitiveCancellation && cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException("refresh-rotated provider-response-body");
                }
            }

            var rotation = AcceptedCallCount;
            var rotatedRefreshToken = rotation == 1 ? "refresh-rotated" : $"refresh-rotated-{rotation}";
            var result = new CredentialMaterial($"access-rotated-{rotation}", rotatedRefreshToken, DateTimeOffset.UtcNow.AddHours(1));
            IssueRefreshToken(result.RefreshToken);
            return result;
        }
    }

    private sealed class AllowUseAuthorizer : IConnectionUseAuthorizer
    {
        public ConnectionUseRequest? LastBackgroundRequest { get; private set; }

        public Task<bool> AuthorizeAsync(ConnectionUseRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Kind == ConnectionUseKind.BackgroundSystem)
                LastBackgroundRequest = request;

            var identity = request.Principal.Identity;
            var inScope = request.TenantId == TenantId && request.EnvironmentId == EnvironmentId &&
                          (request.Purpose == "manage:connect" ? request.ConnectionId.Length == 0 : request.ConnectionId.Length > 0);
            var allowed = request.Kind switch
            {
                ConnectionUseKind.Human => identity?.IsAuthenticated == true && identity.AuthenticationType == "synthetic" && inScope &&
                                           (request.Purpose == "use"
                                               ? request.Principal.HasClaim("permission", "connections.use")
                                               : request.Principal.HasClaim("permission", "connections.manage")),
                ConnectionUseKind.BackgroundSystem => identity?.IsAuthenticated == true && identity.AuthenticationType == "Elsa.Connections.Server" &&
                                                      request.Principal.HasClaim("elsa:identity-kind", "system") && inScope &&
                                                      request.Purpose is "use" or "manage:reconcile",
                _ => false
            };
            return Task.FromResult(allowed);
        }
    }

    private sealed class Worker : IAsyncDisposable
    {
        private ServiceProvider? _serviceProvider;

        private Worker(ServiceProvider serviceProvider, DefaultTenantAccessor tenantAccessor, StageWriteFault? stageWriteFault)
        {
            _serviceProvider = serviceProvider;
            TenantAccessor = tenantAccessor;
            StageWriteFault = stageWriteFault;
        }

        public IServiceProvider Services => _serviceProvider ?? throw new ObjectDisposedException(nameof(Worker));
        public DefaultTenantAccessor TenantAccessor { get; }
        public StageWriteFault? StageWriteFault { get; }

        public static async Task<Worker> CreateAsync(string databasePath, byte[]? encryptionKey, SyntheticCredentialProvider provider, bool migrate = true, bool failBeforeStageWrite = false, TimeProvider? timeProvider = null, ConnectGenerationGate? connectGenerationGate = null)
        {
            var tenantAccessor = new DefaultTenantAccessor();
            var services = new ServiceCollection();
            var connectionString = $"Data Source={databasePath};Cache=Shared;";
            services.AddLogging();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddSingleton<ITenantAccessor>(tenantAccessor);
            services.Configure<TenantsOptions>(options => options.IsEnabled = true);
            services.AddSingleton<IConnectionUseAuthorizer, AllowUseAuthorizer>();
            services.AddSingleton<IConnectionCredentialProvider>(provider);
            services.AddSingleton<TimeProvider>(timeProvider ?? TimeProvider.System);
            var module = services.CreateModule();
            var secretsFeature = module.Configure<SecretsFeature>();
            secretsFeature.ConfigureOptions = options => options.EncryptionKey = encryptionKey;
            secretsFeature.UseEntityFrameworkCore(feature => feature.UseSqlite(connectionString));
            module.Configure<ConnectionsFeature>();
            module.Configure<EFCoreConnectionsPersistenceFeature>(feature => feature.UseSqlite(connectionString));
            module.Apply();
            StageWriteFault? stageWriteFault = failBeforeStageWrite ? new StageWriteFault() : null;
            if (stageWriteFault == null)
                services.AddScoped<IConnectionLifecycleStore>(sp => sp.GetRequiredService<EFCoreConnectionLifecycleStore>());
            else
            {
                services.AddSingleton(stageWriteFault);
                services.AddScoped<IConnectionLifecycleStore>(sp => new FaultingConnectionStore(sp.GetRequiredService<EFCoreConnectionLifecycleStore>(), stageWriteFault));
            }
            if (connectGenerationGate != null)
            {
                services.AddSingleton(connectGenerationGate);
                services.AddScoped<IManagedSecretManager>(sp => new BlockingManagedSecretManager(sp.GetRequiredService<DefaultSecretManager>(), connectGenerationGate));
            }
            var serviceProvider = services.BuildServiceProvider();
            var worker = new Worker(serviceProvider, tenantAccessor, stageWriteFault);
            if (migrate)
            {
                await using var connectionContext = await serviceProvider.GetRequiredService<IDbContextFactory<ConnectionsElsaDbContext>>().CreateDbContextAsync();
                await connectionContext.Database.MigrateAsync();
                await using var secretsContext = await serviceProvider.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>().CreateDbContextAsync();
                await secretsContext.Database.MigrateAsync();
            }

            return worker;
        }

        public async Task StopAsync()
        {
            var serviceProvider = Interlocked.Exchange(ref _serviceProvider, null);
            if (serviceProvider != null)
                await serviceProvider.DisposeAsync();
        }

        public ValueTask DisposeAsync() => new(StopAsync());
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        public TestDatabase() => Path = System.IO.Path.Join(System.IO.Path.GetTempPath(), $"elsa-connections-{Guid.NewGuid():N}.db");

        public string Path { get; }

        public ValueTask DisposeAsync()
        {
            foreach (var file in new[] { Path, $"{Path}-shm", $"{Path}-wal" })
            {
                if (File.Exists(file))
                    File.Delete(file);
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class StageWriteFault
    {
        private int _failNextStage;
        public void FailNextStageWrite() => Interlocked.Exchange(ref _failNextStage, 1);
        public bool ShouldFailStageWrite() => Interlocked.Exchange(ref _failNextStage, 0) == 1;
    }

    private sealed class FaultingConnectionStore(EFCoreConnectionLifecycleStore store, StageWriteFault stageWriteFault) : IConnectionLifecycleStore
    {

        public Task CreateAsync(IntegrationConnection connection, CancellationToken cancellationToken = default) => store.CreateAsync(connection, cancellationToken);
        public Task<IntegrationConnection?> FindAsync(string id, string tenantId, string environmentId, CancellationToken cancellationToken = default) => store.FindAsync(id, tenantId, environmentId, cancellationToken);
        public Task<IntegrationConnection?> TryClaimRefreshAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, DateTimeOffset leaseExpiresAt, CancellationToken cancellationToken = default) => store.TryClaimRefreshAsync(id, tenantId, environmentId, expectedRevision, operationId, leaseExpiresAt, cancellationToken);
        public Task<bool> TryStartProviderCallAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default) => store.TryStartProviderCallAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, cancellationToken);
        public Task<bool> TryRecordStagedGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, string secretName, string generationId, CancellationToken cancellationToken = default)
        {
            if (stageWriteFault.ShouldFailStageWrite())
                throw new TimeoutException("synthetic lost stage write response");
            return store.TryRecordStagedGenerationAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, secretName, generationId, cancellationToken);
        }
        public Task<bool> TryPublishGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default) => store.TryPublishGenerationAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, cancellationToken);
        public Task<bool> TryPromoteRecoveryGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default) => store.TryPromoteRecoveryGenerationAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, cancellationToken);
        public Task<bool> MarkRecoveryRequiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, string safeErrorCode, CancellationToken cancellationToken = default) => store.MarkRecoveryRequiredAsync(id, tenantId, environmentId, operationId, fence, safeErrorCode, cancellationToken);
        public Task<bool> TryMarkRecoveryRequiredIfLeaseExpiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, DateTimeOffset now, string safeErrorCode, CancellationToken cancellationToken = default) => store.TryMarkRecoveryRequiredIfLeaseExpiredAsync(id, tenantId, environmentId, operationId, fence, now, safeErrorCode, cancellationToken);
        public Task<bool> TryDisconnectAsync(string id, string tenantId, string environmentId, long expectedRevision, CancellationToken cancellationToken = default) => store.TryDisconnectAsync(id, tenantId, environmentId, expectedRevision, cancellationToken);
    }

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private long _utcTicks = utcNow.UtcDateTime.Ticks;

        public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _utcTicks), TimeSpan.Zero);

        public void Advance(TimeSpan amount) => Interlocked.Add(ref _utcTicks, amount.Ticks);
    }

    private sealed class ConnectGenerationGate
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => _entered.Task;
        public Task WaitForRelease => _release.Task;
        public void SignalEntered() => _entered.TrySetResult();
        public void Release() => _release.TrySetResult();
    }

    private sealed class BlockingManagedSecretManager(DefaultSecretManager inner, ConnectGenerationGate gate) : IManagedSecretManager
    {
        public async Task<Secret> CreateGenerationAsync(string ownerId, string generationId, string value, CancellationToken cancellationToken = default)
        {
            gate.SignalEntered();
            await gate.WaitForRelease.WaitAsync(cancellationToken);
            return await inner.CreateGenerationAsync(ownerId, generationId, value, cancellationToken);
        }

        public Task<SecretPayload> ResolveGenerationAsync(string name, string ownerId, string generationId, CancellationToken cancellationToken = default) =>
            inner.ResolveGenerationAsync(name, ownerId, generationId, cancellationToken);

        public Task<bool> DeleteGenerationAsync(string name, string ownerId, string generationId, CancellationToken cancellationToken = default) =>
            inner.DeleteGenerationAsync(name, ownerId, generationId, cancellationToken);
    }
}
