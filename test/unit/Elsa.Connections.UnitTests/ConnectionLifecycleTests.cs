using System.Security.Claims;
using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Persistence.EFCore.Features;
using Elsa.Connections.Credentials.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Connections.Features;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.EntityHandlers;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Features;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Secrets.Services;
using Elsa.Tenants.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

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

    [Fact]
    public async Task LifecycleServiceCanResolveWithoutMultiTenancy()
    {
        await using var database = new TestDatabase();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.Configure<TenantsOptions>(options => options.IsEnabled = false);
        var module = services.CreateModule();
        var connectionString = $"Data Source={database.Path};Cache=Shared;";
        var secretsFeature = module.Configure<SecretsFeature>();
        secretsFeature.UseEntityFrameworkCore(feature => feature.UseSqlite(connectionString));
        module.Configure<ConnectionsFeature>();
        module.Configure<EFCoreConnectionsPersistenceFeature>(feature => feature.UseSqlite(connectionString));
        module.Apply();
        services.AddSingleton(Substitute.For<IConnectionCredentialProvider>());
        await using var provider = services.BuildServiceProvider();

        Assert.IsType<DefaultTenantAccessor>(provider.GetRequiredService<ITenantAccessor>());
        Assert.NotNull(provider.GetRequiredService<IConnectionLifecycleService>());
    }

    [Fact]
    public async Task ConnectReturnsSafeProviderAccountStatusAndGenerationMetadata()
    {
        await using var database = new TestDatabase();
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider);
        provider.IssueRefreshToken("refresh-initial");

        using var scope = worker.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>().ConnectAsync(
            Principal(),
            new ConnectConnectionRequest(TenantId, EnvironmentId, "synthetic-oauth", "account-test",
                new CredentialMaterial("access-initial", "refresh-initial", DateTimeOffset.UtcNow.AddHours(1))));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.ConnectionId);
        var metadata = Assert.IsType<ConnectionLifecycleMetadata>(result.Connection);
        Assert.Equal(result.ConnectionId, metadata.ConnectionId);
        Assert.Equal("synthetic-oauth", metadata.ProviderId);
        Assert.Equal("account-test", metadata.ProviderAccountId);
        Assert.Equal(ConnectionStatus.Active, metadata.Status);
        Assert.Equal(2, metadata.Revision);
        Assert.NotNull(metadata.GenerationId);
        Assert.DoesNotContain("access-initial", JsonSerializer.Serialize(result));
        Assert.DoesNotContain("refresh-initial", JsonSerializer.Serialize(result));
    }

    [Fact]
    public async Task ApiKeyConnectReplaceRetireAndDisconnectUseManagedGenerationsWithoutOAuthRefresh()
    {
        await using var database = new TestDatabase();
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
        var apiKeys = Assert.IsAssignableFrom<IStaticApiKeyLifecycleService>(lifecycle);
        var secrets = scope.ServiceProvider.GetRequiredService<IManagedSecretManager>();

        var connected = await apiKeys.ConnectApiKeyAsync(Principal(), new ConnectApiKeyConnectionRequest(
            TenantId, EnvironmentId, "synthetic-api-key", "account-test", "synthetic-api-key-v1"));

        Assert.True(connected.Succeeded);
        Assert.DoesNotContain("synthetic-api-key-v1", JsonSerializer.Serialize(connected));
        var connectionId = Assert.IsType<string>(connected.ConnectionId);
        var first = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>()
            .FindAsync(connectionId, TenantId, EnvironmentId);
        var firstGeneration = Assert.IsType<string>(first?.CurrentGenerationId);
        var firstSecretName = Assert.IsType<string>(first?.CurrentSecretName);
        var firstPayload = await secrets.ResolveGenerationAsync(firstSecretName, connectionId, firstGeneration);
        Assert.Contains("synthetic-api-key-v1", firstPayload.Value, StringComparison.Ordinal);

        var replacement = await apiKeys.ReplaceApiKeyAsync(Principal(), TenantId, EnvironmentId, connectionId,
            connected.Revision!.Value, "synthetic-api-key-v2");

        Assert.True(replacement.Succeeded);
        var current = await lifecycle.ResolveForUseAsync(Principal(canManage: false), TenantId, EnvironmentId, connectionId);
        Assert.Equal(ConnectionCredentialKind.ApiKey, current.Kind);
        Assert.Equal("synthetic-api-key-v2", current.AccessToken);
        Assert.Null(current.ExpiresAt);
        Assert.DoesNotContain("synthetic-api-key-v2", current.ToString(), StringComparison.Ordinal);
        Assert.False((await apiKeys.ReplaceApiKeyAsync(Principal(), TenantId, EnvironmentId, connectionId,
            connected.Revision!.Value, "stale-api-key")).Succeeded);
        Assert.Equal("synthetic-api-key-v2", (await lifecycle.ResolveForUseAsync(Principal(canManage: false), TenantId, EnvironmentId, connectionId)).AccessToken);
        Assert.Equal("credential_refresh_unsupported", (await lifecycle.RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId)).SafeErrorCode);
        Assert.False((await lifecycle.RequestTokenRevocationAsync(Principal(), TenantId, EnvironmentId, connectionId, firstGeneration)).Accepted);

        var cleanup = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, firstGeneration);
        Assert.True(cleanup.Succeeded);
        await Assert.ThrowsAnyAsync<Exception>(() => secrets.ResolveGenerationAsync(firstSecretName, connectionId, firstGeneration));

        var disconnect = await lifecycle.DisconnectAsync(Principal(), TenantId, EnvironmentId, connectionId);
        Assert.True(disconnect.Accepted);
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => lifecycle.ResolveForUseAsync(Principal(canManage: false), TenantId, EnvironmentId, connectionId));
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task ConcurrentApiKeyReplacementUsesOneFencedGenerationWinner()
    {
        await using var database = new TestDatabase();
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var apiKeys = scope.ServiceProvider.GetRequiredService<IStaticApiKeyLifecycleService>();
        var connected = await apiKeys.ConnectApiKeyAsync(Principal(), new ConnectApiKeyConnectionRequest(
            TenantId, EnvironmentId, "synthetic-api-key", "account-test", "concurrent-api-key-v1"));
        var connectionId = Assert.IsType<string>(connected.ConnectionId);

        var results = await Task.WhenAll(
            apiKeys.ReplaceApiKeyAsync(Principal(), TenantId, EnvironmentId, connectionId, connected.Revision!.Value, "concurrent-api-key-v2"),
            apiKeys.ReplaceApiKeyAsync(Principal(), TenantId, EnvironmentId, connectionId, connected.Revision.Value, "concurrent-api-key-v3"));

        Assert.Single(results, result => result.Succeeded);
        Assert.Single(results, result => !result.Succeeded);
        var current = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
            .ResolveForUseAsync(Principal(canManage: false), TenantId, EnvironmentId, connectionId);
        Assert.Contains(current.AccessToken, new[] { "concurrent-api-key-v2", "concurrent-api-key-v3" });
        Assert.Equal(ConnectionCredentialKind.ApiKey, current.Kind);
    }

    [Fact]
    public async Task ApiKeyReplacementCanRecoverAfterLostGenerationStageWrite()
    {
        await using var database = new TestDatabase();
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider, failBeforeStageWrite: true);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var apiKeys = scope.ServiceProvider.GetRequiredService<IStaticApiKeyLifecycleService>();
        var connected = await apiKeys.ConnectApiKeyAsync(Principal(), new ConnectApiKeyConnectionRequest(
            TenantId, EnvironmentId, "synthetic-api-key", "account-test", "recover-api-key-v1"));
        var connectionId = Assert.IsType<string>(connected.ConnectionId);
        worker.StageWriteFault!.FailNextStageWrite();

        var replacement = await apiKeys.ReplaceApiKeyAsync(Principal(), TenantId, EnvironmentId, connectionId,
            connected.Revision!.Value, "recover-api-key-v2");

        Assert.False(replacement.Succeeded);
        Assert.Equal("rotation_outcome_unknown", replacement.SafeErrorCode);
        var recovered = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
            .ReconcileAsync(TenantId, EnvironmentId, connectionId);
        Assert.True(recovered.Succeeded);
        var current = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
            .ResolveForUseAsync(Principal(canManage: false), TenantId, EnvironmentId, connectionId);
        Assert.Equal("recover-api-key-v2", current.AccessToken);
        Assert.Equal(ConnectionCredentialKind.ApiKey, current.Kind);
    }

    [Fact]
    public async Task ExpiredApiKeyReplacementWithoutPlannedGenerationRestoresSourceAndCleansLateWriterOrphan()
    {
        await using var database = new TestDatabase();
        var clock = new TestTimeProvider(DateTimeOffset.UtcNow);
        var gate = new LateGenerationCreateGate();
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider,
            timeProvider: clock, lateGenerationCreateGate: gate);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
        var apiKeys = scope.ServiceProvider.GetRequiredService<IStaticApiKeyLifecycleService>();
        var connected = await apiKeys.ConnectApiKeyAsync(Principal(), new ConnectApiKeyConnectionRequest(
            TenantId, EnvironmentId, "synthetic-api-key", "account-test", "api-key-source"));
        var connectionId = Assert.IsType<string>(connected.ConnectionId);
        var store = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
        var initial = await store.FindAsync(connectionId, TenantId, EnvironmentId);
        var sourceGenerationId = Assert.IsType<string>(initial?.CurrentGenerationId);

        gate.Arm();
        var replacementTask = apiKeys.ReplaceApiKeyAsync(Principal(), TenantId, EnvironmentId, connectionId,
            connected.Revision!.Value, "api-key-late-writer");
        await gate.Entered.WaitAsync(TimeSpan.FromSeconds(15));

        var accepted = await store.FindAsync(connectionId, TenantId, EnvironmentId);
        Assert.Equal(CredentialOperationStatus.CredentialReceived, accepted!.OperationStatus);
        var operationId = Assert.IsType<string>(accepted.OperationId);
        var plannedGenerationId = Assert.IsType<string>(accepted.PlannedGenerationId);
        clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));

        var recovery = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
            .ReconcileAsync(TenantId, EnvironmentId, connectionId);
        Assert.True(recovery.Succeeded);
        var restored = await store.FindAsync(connectionId, TenantId, EnvironmentId);
        Assert.Equal(ConnectionStatus.Active, restored!.Status);
        Assert.Equal(CredentialOperationStatus.Completed, restored.OperationStatus);
        Assert.Null(restored.OperationId);
        Assert.Equal(sourceGenerationId, restored.CurrentGenerationId);
        Assert.False(await store.TryRecordStagedGenerationAsync(connectionId, TenantId, EnvironmentId,
            accepted.OperationExpectedRevision, operationId, accepted.OperationFence,
            ManagedSecretNames.ForGeneration(connectionId, plannedGenerationId), plannedGenerationId));
        Assert.Equal("api-key-source", (await lifecycle.ResolveForUseAsync(Principal(), TenantId, EnvironmentId, connectionId)).AccessToken);

        gate.Release();
        var lateWriter = await replacementTask;
        Assert.False(lateWriter.Succeeded);
        var cleanup = await store.FindGenerationCleanupAsync(connectionId, TenantId, EnvironmentId, plannedGenerationId);
        Assert.Equal(ConnectionGenerationCleanupStatus.Deleted, cleanup?.Status);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => scope.ServiceProvider.GetRequiredService<IManagedSecretManager>()
            .ResolveGenerationAsync(ManagedSecretNames.ForGeneration(connectionId, plannedGenerationId), connectionId, plannedGenerationId));
        var afterCleanup = await store.FindAsync(connectionId, TenantId, EnvironmentId);
        Assert.Equal(sourceGenerationId, afterCleanup!.CurrentGenerationId);
    }

    [Theory]
    [InlineData("{\"kind\":99,\"accessToken\":\"malformed-marker\",\"refreshToken\":null,\"accessTokenExpiresAt\":null}")]
    [InlineData("{\"kind\":1,\"accessToken\":\"malformed-marker\",\"refreshToken\":\"unexpected-refresh\",\"accessTokenExpiresAt\":null}")]
    [InlineData("{\"kind\":1,\"accessToken\":\"malformed-marker\",\"refreshToken\":\"\",\"accessTokenExpiresAt\":null}")]
    [InlineData("{\"kind\":1,\"accessToken\":\"malformed-marker\",\"refreshToken\":null,\"accessTokenExpiresAt\":\"2030-01-01T00:00:00Z\"}")]
    public async Task MalformedCredentialEnvelopesAreUnavailableAndCannotBeReplacedAsApiKeys(string envelope)
    {
        await using var database = new TestDatabase();
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider);
        using var tenant = worker.TenantAccessor.PushContext(TenantContext());
        using var scope = worker.Services.CreateScope();
        var connectionId = Guid.NewGuid().ToString("N");
        var generationId = "malformed-credential-generation";
        var secretName = ManagedSecretNames.ForGeneration(connectionId, generationId);
        await scope.ServiceProvider.GetRequiredService<IManagedSecretManager>()
            .CreateGenerationAsync(connectionId, generationId, envelope);
        await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>().CreateAsync(new IntegrationConnection
        {
            Id = connectionId,
            TenantId = TenantId,
            EnvironmentId = EnvironmentId,
            ProviderId = "synthetic-api-key",
            ProviderAccountId = "account-test",
            CurrentSecretName = secretName,
            CurrentGenerationId = generationId,
            Revision = 1,
            Status = ConnectionStatus.Active,
            OperationStatus = CredentialOperationStatus.Completed
        });

        var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => lifecycle.ResolveForUseAsync(
            Principal(), TenantId, EnvironmentId, connectionId));
        var replace = await scope.ServiceProvider.GetRequiredService<IStaticApiKeyLifecycleService>()
            .ReplaceApiKeyAsync(Principal(), TenantId, EnvironmentId, connectionId, 1, "replacement");
        Assert.False(replace.Succeeded);
        Assert.Equal("connection_unavailable", replace.SafeErrorCode);
    }

    [Fact]
    public async Task DisconnectAndExplicitOffboardingAreDurableScopedAndKeepRevocationGenerationPinned()
    {
        await using var database = new TestDatabase();
        var provider = new SyntheticCredentialProvider(block: false);
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider, timeProvider: timeProvider);
        var connectionId = await SeedAsync(worker);
        string retiredGenerationId;
        string currentGenerationId;

        using (worker.TenantAccessor.PushContext(TenantContext()))
        using (var scope = worker.Services.CreateScope())
        {
            var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
            var store = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var initial = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            retiredGenerationId = initial!.CurrentGenerationId!;
            var refresh = await lifecycle.RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.True(refresh.Succeeded);
            currentGenerationId = refresh.Connection!.GenerationId!;

            var earlyRevocation = await lifecycle.RequestTokenRevocationAsync(Principal(), TenantId, EnvironmentId, connectionId, retiredGenerationId);
            var earlyUninstall = await lifecycle.RequestInstallationUninstallAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.False(earlyRevocation.Accepted);
            Assert.False(earlyUninstall.Accepted);

            var disconnected = await lifecycle.DisconnectAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.True(disconnected.Accepted);
            Assert.Equal(ConnectionOffboardingOperationStatus.Completed, disconnected.Status);
            Assert.Equal(5, disconnected.ConnectionRevision);

            var repeatedDisconnect = await lifecycle.DisconnectAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.Equal(disconnected.OperationId, repeatedDisconnect.OperationId);
            Assert.Equal(disconnected.ConnectionRevision, repeatedDisconnect.ConnectionRevision);
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => lifecycle.ResolveForUseAsync(Principal(), TenantId, EnvironmentId, connectionId));

            var revocation = await lifecycle.RequestTokenRevocationAsync(Principal(), TenantId, EnvironmentId, connectionId, retiredGenerationId);
            var currentRevocation = await lifecycle.RequestTokenRevocationAsync(Principal(), TenantId, EnvironmentId, connectionId, currentGenerationId);
            var uninstall = await lifecycle.RequestInstallationUninstallAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.True(revocation.Accepted);
            Assert.Equal(ConnectionOffboardingOperationStatus.Pending, revocation.Status);
            Assert.True(currentRevocation.Accepted);
            Assert.True(uninstall.Accepted);
            var uninstallRow = await store.FindOffboardingOperationAsync(uninstall.OperationId!, TenantId, EnvironmentId, connectionId);
            Assert.Null(uninstallRow!.GenerationId);
            Assert.DoesNotContain("access-initial", JsonSerializer.Serialize(revocation));
            Assert.DoesNotContain("refresh-initial", JsonSerializer.Serialize(uninstall));

            var pinnedCleanup = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, retiredGenerationId);
            Assert.False(pinnedCleanup.Succeeded);
            Assert.Equal("generation_in_use", pinnedCleanup.SafeErrorCode);
            var pinnedCurrentCleanup = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, currentGenerationId);
            Assert.False(pinnedCurrentCleanup.Succeeded);
            Assert.Equal("generation_in_use", pinnedCurrentCleanup.SafeErrorCode);

            var recovery = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>();
            Assert.True((await recovery.ReconcileOffboardingAsync(TenantId, EnvironmentId, connectionId)).Accepted);
            Assert.True((await recovery.ReconcileOffboardingAsync(TenantId, EnvironmentId, connectionId)).Accepted);
            Assert.True((await recovery.ReconcileOffboardingAsync(TenantId, EnvironmentId, connectionId)).Accepted);
            Assert.Equal(2, provider.RevocationEffects);
            Assert.Equal(1, provider.UninstallEffects);

            var completedCleanup = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, retiredGenerationId);
            Assert.True(completedCleanup.Succeeded);
            var completedCurrentCleanup = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, currentGenerationId);
            Assert.True(completedCurrentCleanup.Succeeded);
            Assert.Equal(ConnectionStatus.Disconnected,
                (await store.FindAsync(connectionId, TenantId, EnvironmentId))!.Status);
            Assert.Null((await store.FindAsync(connectionId, TenantId, EnvironmentId))!.CurrentGenerationId);
        }
    }

    [Fact]
    public async Task TwoWorkersRaceDisconnectAndUninstallIntoSingleStableOperationRecords()
    {
        await using var database = new TestDatabase();
        var provider = new SyntheticCredentialProvider(block: false);
        await using var first = await Worker.CreateAsync(database.Path, MakeKey(32), provider);
        var connectionId = await SeedAsync(first);
        await using var second = await Worker.CreateAsync(database.Path, MakeKey(32), provider, migrate: false);

        async Task<ConnectionOffboardingOperationResult> DisconnectAsync(Worker worker)
        {
            using var scope = worker.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .DisconnectAsync(Principal(), TenantId, EnvironmentId, connectionId);
        }

        var disconnectResults = await Task.WhenAll(DisconnectAsync(first), DisconnectAsync(second));
        Assert.All(disconnectResults, result => Assert.True(result.Accepted));
        Assert.Equal(disconnectResults[0].OperationId, disconnectResults[1].OperationId);

        async Task<ConnectionOffboardingOperationResult> RequestUninstallAsync(Worker worker)
        {
            using var scope = worker.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .RequestInstallationUninstallAsync(Principal(), TenantId, EnvironmentId, connectionId);
        }

        var uninstallResults = await Task.WhenAll(RequestUninstallAsync(first), RequestUninstallAsync(second));
        Assert.All(uninstallResults, result => Assert.True(result.Accepted));
        Assert.Equal(uninstallResults[0].OperationId, uninstallResults[1].OperationId);
        using (first.TenantAccessor.PushContext(TenantContext()))
        using (var scope = first.Services.CreateScope())
        {
            var operation = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>()
                .FindOffboardingOperationAsync(uninstallResults[0].OperationId!, TenantId, EnvironmentId, connectionId);
            Assert.Equal(ConnectionOffboardingOperationKind.InstallationUninstall, operation!.Kind);
            Assert.Equal(ConnectionOffboardingOperationStatus.Pending, operation.Status);
        }
    }

    [Fact]
    public async Task UnknownRevocationOutcomeRetriesAfterRestartWithStableOperationIdAndKeepsGenerationPinned()
    {
        await using var database = new TestDatabase();
        var databasePath = database.Path;
        var key = MakeKey(32);
        var provider = new SyntheticCredentialProvider(block: false);
        provider.ReturnUnknownOnNextRevocation();
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var worker = await Worker.CreateAsync(databasePath, key, provider, timeProvider: timeProvider);
        var connectionId = await SeedAsync(worker);
        string generationId;
        string operationId;

        using (worker.TenantAccessor.PushContext(TenantContext()))
        using (var scope = worker.Services.CreateScope())
        {
            var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
            var connection = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>().FindAsync(connectionId, TenantId, EnvironmentId);
            generationId = connection!.CurrentGenerationId!;
            Assert.True((await lifecycle.RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId)).Succeeded);
            Assert.True((await lifecycle.DisconnectAsync(Principal(), TenantId, EnvironmentId, connectionId)).Accepted);
            var request = await lifecycle.RequestTokenRevocationAsync(Principal(), TenantId, EnvironmentId, connectionId, generationId);
            operationId = request.OperationId!;
            Assert.True((await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
                .ReconcileOffboardingAsync(TenantId, EnvironmentId, connectionId)).Accepted);
            var operation = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>()
                .FindOffboardingOperationAsync(operationId, TenantId, EnvironmentId, connectionId);
            Assert.Equal(ConnectionOffboardingOperationStatus.UnknownOutcome, operation!.Status);

            var cleanup = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, generationId);
            Assert.False(cleanup.Succeeded);
            Assert.Equal("generation_in_use", cleanup.SafeErrorCode);
        }

        timeProvider.Advance(TimeSpan.FromSeconds(31));
        await worker.StopAsync();
        await using var restarted = await Worker.CreateAsync(databasePath, key, provider, migrate: false, timeProvider: timeProvider);
        using (restarted.TenantAccessor.PushContext(TenantContext()))
        using (var scope = restarted.Services.CreateScope())
        {
            var recovery = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>();
            var result = await recovery.ReconcileOffboardingAsync(TenantId, EnvironmentId, connectionId);
            Assert.True(result.Accepted);
            Assert.Equal(operationId, result.OperationId);
            Assert.Equal(ConnectionOffboardingOperationStatus.Completed, result.Status);
            Assert.Equal(2, provider.RevocationAttempts);
            Assert.Equal(1, provider.RevocationEffects);
            var cleanup = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, generationId);
            Assert.True(cleanup.Succeeded);
        }
    }

    [Fact]
    public async Task UnknownOutcomeFromNonIdempotentProviderIsPersistedAndNeverAutomaticallyReplayed()
    {
        await using var database = new TestDatabase();
        var provider = new SyntheticCredentialProvider(block: false, supportsStableOperationIdIdempotency: false);
        provider.ReturnUnknownOnNextRevocation();
        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider, timeProvider: timeProvider);
        var connectionId = await SeedAsync(worker);

        using (worker.TenantAccessor.PushContext(TenantContext()))
        using (var scope = worker.Services.CreateScope())
        {
            var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
            var recovery = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>();
            var store = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var connection = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            var generationId = connection!.CurrentGenerationId!;
            await lifecycle.DisconnectAsync(Principal(), TenantId, EnvironmentId, connectionId);
            var requested = await lifecycle.RequestTokenRevocationAsync(Principal(), TenantId, EnvironmentId, connectionId, generationId);

            var first = await recovery.ReconcileOffboardingAsync(TenantId, EnvironmentId, connectionId);
            Assert.Equal(ConnectionOffboardingOperationStatus.UnknownOutcome, first.Status);
            timeProvider.Advance(TimeSpan.FromMinutes(5));
            var next = await recovery.ReconcileOffboardingAsync(TenantId, EnvironmentId, connectionId);
            Assert.False(next.Accepted);
            Assert.Equal("offboarding_outcome_unknown", next.SafeErrorCode);
            Assert.Equal(requested.OperationId, next.OperationId);
            Assert.Equal(1, provider.RevocationAttempts);
            Assert.Equal(1, provider.RevocationEffects);
        }
    }

    [Fact]
    public async Task CallerCancellationAfterSuccessfulNonIdempotentRevocationDoesNotLoseConfirmedCompletion()
    {
        await using var database = new TestDatabase();
        using var cancellation = new CancellationTokenSource();
        var provider = new SyntheticCredentialProvider(
            block: false,
            supportsStableOperationIdIdempotency: false,
            cancelBeforeSuccessfulRevocation: cancellation.Cancel);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider);
        var connectionId = await SeedAsync(worker);

        using (worker.TenantAccessor.PushContext(TenantContext()))
        using (var scope = worker.Services.CreateScope())
        {
            var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
            var recovery = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>();
            var store = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var generationId = (await store.FindAsync(connectionId, TenantId, EnvironmentId))!.CurrentGenerationId!;
            Assert.True((await lifecycle.DisconnectAsync(Principal(), TenantId, EnvironmentId, connectionId)).Accepted);
            var requested = await lifecycle.RequestTokenRevocationAsync(Principal(), TenantId, EnvironmentId, connectionId, generationId);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                recovery.ReconcileOffboardingAsync(TenantId, EnvironmentId, connectionId, cancellation.Token));

            var operation = await store.FindOffboardingOperationAsync(requested.OperationId!, TenantId, EnvironmentId, connectionId);
            Assert.Equal(ConnectionOffboardingOperationStatus.Completed, operation!.Status);
            Assert.Equal(1, provider.RevocationEffects);

            var cleanup = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, generationId);
            Assert.True(cleanup.Succeeded);
        }
    }

    [Fact]
    public async Task DisconnectDuringCredentialReadDeniesTheNewCredentialHandoff()
    {
        await using var database = new TestDatabase();
        var key = MakeKey(32);
        var provider = new SyntheticCredentialProvider(block: false);
        await using var disconnectWorker = await Worker.CreateAsync(database.Path, key, provider);
        var connectionId = await SeedAsync(disconnectWorker);
        var readGate = new ConnectGenerationGate();
        await using var resolveWorker = await Worker.CreateAsync(database.Path, key, provider, migrate: false, secretResolveGate: readGate);

        using var resolveScope = resolveWorker.Services.CreateScope();
        var resolving = resolveScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
            .ResolveForUseAsync(Principal(), TenantId, EnvironmentId, connectionId);
        await readGate.Entered.WaitAsync(TimeSpan.FromSeconds(15));

        using (disconnectWorker.TenantAccessor.PushContext(TenantContext()))
        using (var disconnectScope = disconnectWorker.Services.CreateScope())
        {
            var disconnected = await disconnectScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .DisconnectAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.True(disconnected.Accepted);
        }

        readGate.Release();
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => resolving);
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
        ConnectionLifecycleResult refreshed;

        using (var scope1 = worker1.Services.CreateScope())
        using (var scope2 = worker2.Services.CreateScope())
        {
            var firstRefresh = scope1.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
            await provider.Entered.WaitAsync(TimeSpan.FromSeconds(15));

            var concurrentRefresh = await scope2.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.False(concurrentRefresh.Succeeded);
            Assert.Equal(3, concurrentRefresh.Revision);
            Assert.NotNull(concurrentRefresh.Connection);
            provider.Complete(new CredentialMaterial("access-rotated", "refresh-rotated", DateTimeOffset.UtcNow.AddHours(1)));

            refreshed = await firstRefresh;
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
        Assert.Equal(4, refreshed.Revision);
        Assert.NotNull(refreshed.Connection?.GenerationId);

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
            var lifecycle = disconnectScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
            var claimed = await disconnectScope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>()
                .FindAsync(connectionId, TenantId, EnvironmentId);
            Assert.NotNull(claimed);
            Assert.True((await lifecycle.DisconnectAsync(Principal(), TenantId, EnvironmentId, connectionId)).Accepted);
            var currentCleanup = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, claimed.CurrentGenerationId!);
            Assert.False(currentCleanup.Succeeded);
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
            var cleanup = await reconcileScope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>()
                .CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, before.CurrentGenerationId!);
            Assert.False(cleanup.Succeeded);

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
    public async Task PreProviderSecretResolutionFailureReleasesClaimAndKeepsCurrentCredentialUsable()
    {
        await using var database = new TestDatabase();
        var provider = new SyntheticCredentialProvider(block: false);
        var resolveFault = new SecretResolveFault();
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider, secretResolveFault: resolveFault);
        var connectionId = await SeedAsync(worker);
        resolveFault.FailNextResolve();

        using (worker.TenantAccessor.PushContext(TenantContext()))
        using (var scope = worker.Services.CreateScope())
        {
            var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
            var store = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var before = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            var result = await lifecycle.RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
            var after = await store.FindAsync(connectionId, TenantId, EnvironmentId);

            Assert.False(result.Succeeded);
            Assert.Equal("refresh_not_started", result.SafeErrorCode);
            Assert.Equal(ConnectionStatus.Active, after!.Status);
            Assert.Equal(CredentialOperationStatus.Completed, after.OperationStatus);
            Assert.Null(after.OperationId);
            Assert.Equal(before!.CurrentGenerationId, after.CurrentGenerationId);
            Assert.Equal(0, provider.CallCount);
            Assert.Equal("access-initial", (await lifecycle.ResolveForUseAsync(Principal(), TenantId, EnvironmentId, connectionId)).AccessToken);

            var retry = await lifecycle.RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.True(retry.Succeeded);
            Assert.Equal(1, provider.CallCount);
        }
    }

    [Fact]
    public async Task ExpiredUnstartedRefreshClaimReleasesWithoutDisablingOrCallingProvider()
    {
        await using var database = new TestDatabase();
        var clock = new TestTimeProvider(DateTimeOffset.UtcNow);
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider, timeProvider: clock);
        var connectionId = await SeedAsync(worker);

        using (worker.TenantAccessor.PushContext(TenantContext()))
        using (var scope = worker.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var original = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            var operationId = $"unstarted-{Guid.NewGuid():N}";
            var claimed = await store.TryClaimCredentialUpdateAsync(
                connectionId, TenantId, EnvironmentId, original!.Revision, operationId, clock.GetUtcNow() + TimeSpan.FromMinutes(2));
            Assert.NotNull(claimed);

            var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>();
            var beforeExpiry = await lifecycle.ReconcileAsync(TenantId, EnvironmentId, connectionId);
            Assert.Equal("operation_in_progress", beforeExpiry.SafeErrorCode);

            clock.Advance(TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1));
            var expired = await lifecycle.ReconcileAsync(TenantId, EnvironmentId, connectionId);
            Assert.Equal("refresh_not_started", expired.SafeErrorCode);

            var recovered = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            Assert.Equal(ConnectionStatus.Active, recovered!.Status);
            Assert.Equal(CredentialOperationStatus.Completed, recovered.OperationStatus);
            Assert.Null(recovered.OperationId);
            Assert.Equal(original.CurrentGenerationId, recovered.CurrentGenerationId);
            Assert.False(await store.TryStartProviderCallAsync(
                connectionId, TenantId, EnvironmentId, claimed.OperationExpectedRevision, operationId, claimed.OperationFence, clock.GetUtcNow()));
            Assert.Equal(0, provider.CallCount);

            var connectionLifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
            Assert.Equal("access-initial", (await connectionLifecycle.ResolveForUseAsync(Principal(), TenantId, EnvironmentId, connectionId)).AccessToken);
            Assert.True((await connectionLifecycle.RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId)).Succeeded);
            Assert.Equal(1, provider.CallCount);
        }
    }

    [Fact]
    public async Task DisconnectDuringUnstartedRefreshClaimClearsOperationWithoutRevivingConnection()
    {
        await using var database = new TestDatabase();
        var clock = new TestTimeProvider(DateTimeOffset.UtcNow);
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider, timeProvider: clock);
        var connectionId = await SeedAsync(worker);

        using (worker.TenantAccessor.PushContext(TenantContext()))
        using (var scope = worker.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var original = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            var operationId = $"unstarted-disconnect-{Guid.NewGuid():N}";
            var claimed = await store.TryClaimCredentialUpdateAsync(
                connectionId, TenantId, EnvironmentId, original!.Revision, operationId, clock.GetUtcNow() + TimeSpan.FromMinutes(2));
            Assert.NotNull(claimed);
            Assert.True(await store.TryDisconnectAsync(connectionId, TenantId, EnvironmentId, claimed.Revision));

            var result = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleRecoveryService>()
                .ReconcileAsync(TenantId, EnvironmentId, connectionId);
            var disconnected = await store.FindAsync(connectionId, TenantId, EnvironmentId);

            Assert.False(result.Succeeded);
            Assert.Equal("connection_unavailable", result.SafeErrorCode);
            Assert.Equal(ConnectionStatus.Disconnected, disconnected!.Status);
            Assert.Equal(CredentialOperationStatus.Completed, disconnected.OperationStatus);
            Assert.Null(disconnected.OperationId);
            Assert.Equal(original.CurrentGenerationId, disconnected.CurrentGenerationId);
            Assert.Equal(0, provider.CallCount);
        }
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
            var claimed = await store.TryClaimCredentialUpdateAsync(connectionId, TenantId, EnvironmentId, current!.Revision, operationId, clock.GetUtcNow() + TimeSpan.FromMinutes(2));
            Assert.NotNull(claimed);
            Assert.True(await store.TryStartProviderCallAsync(connectionId, TenantId, EnvironmentId, claimed.OperationExpectedRevision, operationId, claimed.OperationFence, DateTimeOffset.UtcNow));
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

    [Fact]
    public async Task CleanupRejectsCurrentAndUnresolvedStagedGenerations()
    {
        await using var database = new TestDatabase();
        var provider = new SyntheticCredentialProvider(block: true);
        await using var worker = await Worker.CreateAsync(database.Path, MakeKey(32), provider);
        var connectionId = await SeedAsync(worker);
        using var scope = worker.Services.CreateScope();
        var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
        var store = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
        var managedSecrets = scope.ServiceProvider.GetRequiredService<IManagedSecretManager>();

        using (worker.TenantAccessor.PushContext(TenantContext()))
        {
            var connection = await store.FindAsync(connectionId, TenantId, EnvironmentId);
            Assert.NotNull(connection);
            var currentCleanup = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, connection.CurrentGenerationId!);
            Assert.False(currentCleanup.Succeeded);
            Assert.Equal("generation_in_use", currentCleanup.SafeErrorCode);
            var retainedCurrent = (await managedSecrets.ResolveGenerationAsync(
                connection.CurrentSecretName!, connectionId, connection.CurrentGenerationId!)).Value;
            Assert.Contains("access-initial", retainedCurrent);

            var operationId = $"staged-{Guid.NewGuid():N}";
            var claimed = await store.TryClaimCredentialUpdateAsync(connectionId, TenantId, EnvironmentId, connection.Revision, operationId, DateTimeOffset.UtcNow.AddMinutes(2));
            Assert.NotNull(claimed);
            Assert.True(await store.TryStartProviderCallAsync(connectionId, TenantId, EnvironmentId, claimed.OperationExpectedRevision, operationId, claimed.OperationFence, DateTimeOffset.UtcNow));
            var stagedName = ManagedSecretNames.ForGeneration(connectionId, operationId);
            await managedSecrets.CreateGenerationAsync(connectionId, operationId, "synthetic-staged-envelope");
            Assert.True(await store.TryRecordStagedGenerationAsync(connectionId, TenantId, EnvironmentId,
                claimed.OperationExpectedRevision, operationId, claimed.OperationFence, stagedName, operationId));

            var stagedCleanup = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, operationId);
            Assert.False(stagedCleanup.Succeeded);
            Assert.Equal("generation_in_use", stagedCleanup.SafeErrorCode);
            Assert.Null(await store.FindGenerationCleanupAsync(connectionId, TenantId, EnvironmentId, operationId));
            Assert.Equal("synthetic-staged-envelope", (await managedSecrets.ResolveGenerationAsync(stagedName, connectionId, operationId)).Value);
        }
    }

    [Fact]
    public async Task CleanupUsesRevisionCasAndRejectsWrongOwnerWithoutDeletingTheirGeneration()
    {
        await using var database = new TestDatabase();
        var key = MakeKey(32);
        var provider = new SyntheticCredentialProvider(block: false);
        await using var worker1 = await Worker.CreateAsync(database.Path, key, provider);
        var connectionId = await SeedAsync(worker1);
        await using var worker2 = await Worker.CreateAsync(database.Path, key, provider, migrate: false);

        string retiredGenerationId;
        using (var scope = worker1.Services.CreateScope())
        {
            var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
            using var tenant = worker1.TenantAccessor.PushContext(TenantContext());
            var original = await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>().FindAsync(connectionId, TenantId, EnvironmentId);
            retiredGenerationId = original!.CurrentGenerationId!;
            var refreshed = await lifecycle.RefreshAsync(Principal(), TenantId, EnvironmentId, connectionId);
            Assert.True(refreshed.Succeeded);
            Assert.NotEqual(retiredGenerationId, refreshed.Connection!.GenerationId);
        }

        async Task<ConnectionLifecycleResult> CleanupAsync(Worker worker)
        {
            using var scope = worker.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>().CleanupGenerationAsync(
                Principal(), TenantId, EnvironmentId, connectionId, retiredGenerationId);
        }

        var cleanupResults = await Task.WhenAll(CleanupAsync(worker1), CleanupAsync(worker2));
        Assert.Contains(cleanupResults, result => result.Succeeded);
        Assert.All(cleanupResults.Where(result => !result.Succeeded), result =>
            Assert.Contains(result.SafeErrorCode, new[] { "generation_cleanup_in_progress", "generation_in_use" }));

        using (worker1.TenantAccessor.PushContext(TenantContext()))
        using (var scope = worker1.Services.CreateScope())
        {
            var lifecycle = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleService>();
            var store = scope.ServiceProvider.GetRequiredService<IConnectionLifecycleStore>();
            var cleanupRecord = await store.FindGenerationCleanupAsync(connectionId, TenantId, EnvironmentId, retiredGenerationId);
            Assert.Equal(ConnectionGenerationCleanupStatus.Deleted, cleanupRecord!.Status);
            Assert.Equal("access-rotated-1", (await lifecycle.ResolveForUseAsync(Principal(), TenantId, EnvironmentId, connectionId)).AccessToken);

            var manager = scope.ServiceProvider.GetRequiredService<IManagedSecretManager>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => manager.CreateGenerationAsync(connectionId, retiredGenerationId, "attempted-generation-reuse"));
            var foreignGenerationId = "foreign-generation";
            var foreignOwnerId = "foreign-connection";
            var foreignSecret = await manager.CreateGenerationAsync(foreignOwnerId, foreignGenerationId, "foreign-secret-value");
            var wrongOwnerDelete = await manager.DeleteGenerationAsync(foreignSecret.Name, connectionId, foreignGenerationId);
            Assert.False(wrongOwnerDelete);

            var lifecycleWrongOwnerDelete = await lifecycle.CleanupGenerationAsync(Principal(), TenantId, EnvironmentId, connectionId, foreignGenerationId);
            Assert.False(lifecycleWrongOwnerDelete.Succeeded);
            Assert.Equal("generation_unavailable", lifecycleWrongOwnerDelete.SafeErrorCode);
            Assert.Equal("foreign-secret-value", (await manager.ResolveGenerationAsync(foreignSecret.Name, foreignOwnerId, foreignGenerationId)).Value);
        }
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
        {
            claims.Add(new Claim("permission", "connections.use"));
        }
        if (canManage)
        {
            claims.Add(new Claim("permission", "connections.manage"));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "synthetic"));
    }
    private static Tenant TenantContext() => new() { Id = TenantId, Name = TenantId };
    private static byte[] MakeKey(int length, bool mismatch = false) => Enumerable.Range(1, length).Select(x => (byte)(x + (mismatch ? 1 : 0))).ToArray();

    private sealed class SyntheticCredentialProvider(
        bool block,
        bool throwSensitiveCancellation = false,
        bool supportsStableOperationIdIdempotency = true,
        Action? cancelBeforeSuccessfulRevocation = null) : IConnectionCredentialProvider, IConnectionOffboardingProvider
    {
        private readonly TaskCompletionSource<CredentialMaterial> _response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _gate = new();
        private readonly HashSet<string> _availableTokens = new(StringComparer.Ordinal);
        private readonly HashSet<string> _consumedTokens = new(StringComparer.Ordinal);
        private readonly HashSet<string> _completedOffboardingOperationIds = new(StringComparer.Ordinal);
        private readonly Queue<ConnectionOffboardingProviderResult> _nextRevocationResults = new();
        private int _callCount;
        private int _acceptedCallCount;
        private int _revocationAttempts;
        private int _revocationEffects;
        private int _uninstallAttempts;
        private int _uninstallEffects;

        public int CallCount => Volatile.Read(ref _callCount);
        public int AcceptedCallCount => Volatile.Read(ref _acceptedCallCount);
        public int RevocationAttempts => Volatile.Read(ref _revocationAttempts);
        public int RevocationEffects => Volatile.Read(ref _revocationEffects);
        public int UninstallAttempts => Volatile.Read(ref _uninstallAttempts);
        public int UninstallEffects => Volatile.Read(ref _uninstallEffects);
        public bool SupportsStableOperationIdIdempotency(ConnectionOffboardingOperationKind kind) => supportsStableOperationIdIdempotency;
        public Task Entered => _entered.Task;

        public void ReturnUnknownOnNextRevocation()
        {
            lock (_gate)
            {
                _nextRevocationResults.Enqueue(ConnectionOffboardingProviderResult.UnknownOutcome);
            }
        }
        public void IssueRefreshToken(string token)
        {
            lock (_gate)
            {
                if (!_availableTokens.Add(token))
                {
                    throw new InvalidOperationException("synthetic_duplicate_refresh_token");
                }
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
                {
                    throw new InvalidOperationException("synthetic_invalid_grant");
                }

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

        public Task<ConnectionOffboardingProviderResult> RevokeTokenPairAsync(
            string providerId,
            string providerAccountId,
            string operationId,
            CredentialMaterial credentials,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _revocationAttempts);
            ConnectionOffboardingProviderResult result;
            lock (_gate)
            {
                var firstAttempt = _completedOffboardingOperationIds.Add(operationId);
                if (!firstAttempt && supportsStableOperationIdIdempotency)
                {
                    return Task.FromResult(ConnectionOffboardingProviderResult.Succeeded);
                }

                Interlocked.Increment(ref _revocationEffects);
                result = ConnectionOffboardingProviderResult.Succeeded;
                if (firstAttempt && _nextRevocationResults.TryDequeue(out var next))
                {
                    result = next;
                }
            }

            if (result == ConnectionOffboardingProviderResult.Succeeded)
            {
                cancelBeforeSuccessfulRevocation?.Invoke();
            }

            return Task.FromResult(result);
        }

        public Task<ConnectionOffboardingProviderResult> UninstallInstallationAsync(
            string providerId,
            string providerAccountId,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _uninstallAttempts);
            lock (_gate)
            {
                if (_completedOffboardingOperationIds.Add(operationId) || !supportsStableOperationIdIdempotency)
                {
                    Interlocked.Increment(ref _uninstallEffects);
                }

                return Task.FromResult(ConnectionOffboardingProviderResult.Succeeded);
            }
        }
    }

    private sealed class AllowUseAuthorizer : IConnectionUseAuthorizer
    {
        public ConnectionUseRequest? LastBackgroundRequest { get; private set; }

        public Task<bool> AuthorizeAsync(ConnectionUseRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Kind == ConnectionUseKind.BackgroundSystem)
            {
                LastBackgroundRequest = request;
            }

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

        public static async Task<Worker> CreateAsync(string databasePath, byte[]? encryptionKey, SyntheticCredentialProvider provider, bool migrate = true, bool failBeforeStageWrite = false, TimeProvider? timeProvider = null, ConnectGenerationGate? connectGenerationGate = null, SecretResolveFault? secretResolveFault = null, ConnectGenerationGate? secretResolveGate = null, LateGenerationCreateGate? lateGenerationCreateGate = null)
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
            if (provider is IConnectionOffboardingProvider offboardingProvider)
            {
                services.AddSingleton(offboardingProvider);
                services.AddSingleton<IConnectionOffboardingProvider>(offboardingProvider);
            }
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
            {
                services.AddScoped<IConnectionLifecycleStore>(sp => sp.GetRequiredService<EFCoreConnectionLifecycleStore>());
            }
            else
            {
                services.AddSingleton(stageWriteFault);
                services.AddScoped<IConnectionLifecycleStore>(sp => new FaultingConnectionStore(sp.GetRequiredService<EFCoreConnectionLifecycleStore>(), stageWriteFault));
            }
            if (connectGenerationGate != null || secretResolveGate != null)
            {
                if (connectGenerationGate != null)
                {
                    services.AddSingleton(connectGenerationGate);
                }
                if (secretResolveGate != null)
                {
                    services.AddSingleton(secretResolveGate);
                }
                services.AddScoped<IManagedSecretManager>(sp => new BlockingManagedSecretManager(
                    sp.GetRequiredService<DefaultSecretManager>(), connectGenerationGate, secretResolveGate));
            }
            if (secretResolveFault != null)
            {
                services.AddSingleton(secretResolveFault);
                services.AddScoped<IManagedSecretManager>(sp => new FaultingManagedSecretManager(sp.GetRequiredService<DefaultSecretManager>(), secretResolveFault));
            }
            if (lateGenerationCreateGate != null)
            {
                services.AddSingleton(lateGenerationCreateGate);
                services.AddScoped<IManagedSecretManager>(sp => new LateGenerationCreateManagedSecretManager(
                    sp.GetRequiredService<DefaultSecretManager>(), lateGenerationCreateGate));
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
            {
                await serviceProvider.DisposeAsync();
            }
        }

        public ValueTask DisposeAsync() => new(StopAsync());
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        public TestDatabase() => Path = System.IO.Path.Join(System.IO.Path.GetTempPath(), $"elsa-connections-{Guid.NewGuid():N}.db");

        public string Path { get; }

        public ValueTask DisposeAsync()
        {
            foreach (var file in new[] { Path, $"{Path}-shm", $"{Path}-wal" }.Where(File.Exists))
            {
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
        public Task<ConnectionGenerationCleanup?> FindGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, CancellationToken cancellationToken = default) => store.FindGenerationCleanupAsync(connectionId, tenantId, environmentId, generationId, cancellationToken);
        public Task<ConnectionGenerationCleanup?> TryClaimGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, long expectedRevision, string generationId, DateTimeOffset now, DateTimeOffset leaseExpiresAt, CancellationToken cancellationToken = default) => store.TryClaimGenerationCleanupAsync(connectionId, tenantId, environmentId, expectedRevision, generationId, now, leaseExpiresAt, cancellationToken);
        public Task<bool> CompleteGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, long fence, CancellationToken cancellationToken = default) => store.CompleteGenerationCleanupAsync(connectionId, tenantId, environmentId, generationId, fence, cancellationToken);
        public Task<bool> CancelGenerationCleanupAsync(string connectionId, string tenantId, string environmentId, string generationId, long fence, CancellationToken cancellationToken = default) => store.CancelGenerationCleanupAsync(connectionId, tenantId, environmentId, generationId, fence, cancellationToken);
        public Task<IntegrationConnection?> TryDisconnectAndRecordAsync(string id, string tenantId, string environmentId, long expectedRevision, ConnectionOffboardingOperation operation, CancellationToken cancellationToken = default) => store.TryDisconnectAndRecordAsync(id, tenantId, environmentId, expectedRevision, operation, cancellationToken);
        public Task<ConnectionOffboardingOperation?> FindOffboardingOperationAsync(string operationId, string tenantId, string environmentId, string connectionId, CancellationToken cancellationToken = default) => store.FindOffboardingOperationAsync(operationId, tenantId, environmentId, connectionId, cancellationToken);
        public Task<ConnectionOffboardingOperation?> FindNextOffboardingOperationAsync(string tenantId, string environmentId, string connectionId, DateTimeOffset now, bool stableRevocationIdIdempotency, bool stableUninstallIdIdempotency, CancellationToken cancellationToken = default) => store.FindNextOffboardingOperationAsync(tenantId, environmentId, connectionId, now, stableRevocationIdIdempotency, stableUninstallIdIdempotency, cancellationToken);
        public Task<ConnectionOffboardingOperation?> TryQueueOffboardingOperationAsync(long expectedConnectionRevision, ConnectionOffboardingOperation operation, CancellationToken cancellationToken = default) => store.TryQueueOffboardingOperationAsync(expectedConnectionRevision, operation, cancellationToken);
        public Task<ConnectionOffboardingOperation?> TryClaimOffboardingOperationAsync(string operationId, string tenantId, string environmentId, string connectionId, long expectedFence, DateTimeOffset now, DateTimeOffset leaseExpiresAt, CancellationToken cancellationToken = default) => store.TryClaimOffboardingOperationAsync(operationId, tenantId, environmentId, connectionId, expectedFence, now, leaseExpiresAt, cancellationToken);
        public Task<bool> TryStartOffboardingProviderCallAsync(string operationId, string tenantId, string environmentId, string connectionId, long fence, DateTimeOffset now, CancellationToken cancellationToken = default) => store.TryStartOffboardingProviderCallAsync(operationId, tenantId, environmentId, connectionId, fence, now, cancellationToken);
        public Task<bool> TryMarkOffboardingOutcomeUnknownIfLeaseExpiredAsync(string operationId, string tenantId, string environmentId, string connectionId, long fence, DateTimeOffset now, string safeErrorCode, CancellationToken cancellationToken = default) => store.TryMarkOffboardingOutcomeUnknownIfLeaseExpiredAsync(operationId, tenantId, environmentId, connectionId, fence, now, safeErrorCode, cancellationToken);
        public Task<bool> TryReleaseOffboardingClaimAsync(string operationId, string tenantId, string environmentId, string connectionId, long fence, DateTimeOffset updatedAt, DateTimeOffset nextAttemptAt, string safeErrorCode, CancellationToken cancellationToken = default) => store.TryReleaseOffboardingClaimAsync(operationId, tenantId, environmentId, connectionId, fence, updatedAt, nextAttemptAt, safeErrorCode, cancellationToken);
        public Task<bool> TryCompleteOffboardingOperationAsync(string operationId, string tenantId, string environmentId, string connectionId, long fence, DateTimeOffset completedAt, CancellationToken cancellationToken = default) => store.TryCompleteOffboardingOperationAsync(operationId, tenantId, environmentId, connectionId, fence, completedAt, cancellationToken);
        public Task<bool> TryRecordOffboardingFailureAsync(string operationId, string tenantId, string environmentId, string connectionId, long fence, ConnectionOffboardingOperationStatus status, DateTimeOffset updatedAt, DateTimeOffset? nextAttemptAt, string safeErrorCode, CancellationToken cancellationToken = default) => store.TryRecordOffboardingFailureAsync(operationId, tenantId, environmentId, connectionId, fence, status, updatedAt, nextAttemptAt, safeErrorCode, cancellationToken);
        public Task<IntegrationConnection?> TryClaimRefreshAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, DateTimeOffset leaseExpiresAt, CancellationToken cancellationToken = default) => store.TryClaimRefreshAsync(id, tenantId, environmentId, expectedRevision, operationId, leaseExpiresAt, cancellationToken);

        public Task<IntegrationConnection?> TryClaimCredentialUpdateAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, DateTimeOffset leaseExpiresAt, CancellationToken cancellationToken = default) => store.TryClaimCredentialUpdateAsync(id, tenantId, environmentId, expectedRevision, operationId, leaseExpiresAt, cancellationToken);
        public Task<bool> TryAcceptCredentialUpdateAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, DateTimeOffset now, CancellationToken cancellationToken = default) => store.TryAcceptCredentialUpdateAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, now, cancellationToken);
        public Task<bool> TryStartProviderCallAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, DateTimeOffset now, CancellationToken cancellationToken = default) => store.TryStartProviderCallAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, now, cancellationToken);
        public Task<bool> TryReleaseUnstartedRefreshAsync(string id, string tenantId, string environmentId, string operationId, long fence, string safeErrorCode, CancellationToken cancellationToken = default) => store.TryReleaseUnstartedRefreshAsync(id, tenantId, environmentId, operationId, fence, safeErrorCode, cancellationToken);
        public Task<bool> TryReleaseExpiredRefreshClaimAsync(string id, string tenantId, string environmentId, string operationId, long fence, DateTimeOffset now, string safeErrorCode, CancellationToken cancellationToken = default) => store.TryReleaseExpiredRefreshClaimAsync(id, tenantId, environmentId, operationId, fence, now, safeErrorCode, cancellationToken);
        public Task<bool> TryRecordStagedGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, string secretName, string generationId, CancellationToken cancellationToken = default)
        {
            if (stageWriteFault.ShouldFailStageWrite())
            {
                throw new TimeoutException("synthetic lost stage write response");
            }
            return store.TryRecordStagedGenerationAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, secretName, generationId, cancellationToken);
        }
        public Task<bool> TryPublishGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default) => store.TryPublishGenerationAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, cancellationToken);
        public Task<bool> TryPromoteRecoveryGenerationAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, CancellationToken cancellationToken = default) => store.TryPromoteRecoveryGenerationAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, cancellationToken);
        public Task<bool> MarkRecoveryRequiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, string safeErrorCode, CancellationToken cancellationToken = default) => store.MarkRecoveryRequiredAsync(id, tenantId, environmentId, operationId, fence, safeErrorCode, cancellationToken);
        public Task<bool> TryMarkRecoveryRequiredIfLeaseExpiredAsync(string id, string tenantId, string environmentId, string operationId, long fence, DateTimeOffset now, string safeErrorCode, CancellationToken cancellationToken = default) => store.TryMarkRecoveryRequiredIfLeaseExpiredAsync(id, tenantId, environmentId, operationId, fence, now, safeErrorCode, cancellationToken);
        public Task<bool> TryRestoreSourceGenerationAfterMissingPlanAsync(string id, string tenantId, string environmentId, long expectedRevision, string operationId, long fence, string sourceGenerationId, string safeErrorCode, CancellationToken cancellationToken = default) => store.TryRestoreSourceGenerationAfterMissingPlanAsync(id, tenantId, environmentId, expectedRevision, operationId, fence, sourceGenerationId, safeErrorCode, cancellationToken);
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

    private sealed class LateGenerationCreateGate
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _armed;

        public Task Entered => _entered.Task;
        public void Arm() => Interlocked.Exchange(ref _armed, 1);
        public void Release() => _release.TrySetResult();

        public async Task WaitIfArmedAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref _armed, 0) != 1)
            {
                return;
            }

            _entered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class SecretResolveFault
    {
        private int _failNextResolve;

        public void FailNextResolve() => Interlocked.Exchange(ref _failNextResolve, 1);

        public void ThrowIfRequested()
        {
            if (Interlocked.Exchange(ref _failNextResolve, 0) == 1)
            {
                throw new InvalidOperationException("synthetic secret storage failure");
            }
        }
    }

    private sealed class FaultingManagedSecretManager(DefaultSecretManager inner, SecretResolveFault fault) : IManagedSecretManager
    {
        public Task<Secret> CreateGenerationAsync(string ownerId, string generationId, string value, CancellationToken cancellationToken = default) =>
            inner.CreateGenerationAsync(ownerId, generationId, value, cancellationToken);

        public Task<SecretPayload> ResolveGenerationAsync(string name, string ownerId, string generationId, CancellationToken cancellationToken = default)
        {
            fault.ThrowIfRequested();
            return inner.ResolveGenerationAsync(name, ownerId, generationId, cancellationToken);
        }

        public Task<bool> DeleteGenerationAsync(string name, string ownerId, string generationId, CancellationToken cancellationToken = default) =>
            inner.DeleteGenerationAsync(name, ownerId, generationId, cancellationToken);
    }

    private sealed class LateGenerationCreateManagedSecretManager(DefaultSecretManager inner, LateGenerationCreateGate gate) : IManagedSecretManager
    {
        public async Task<Secret> CreateGenerationAsync(string ownerId, string generationId, string value, CancellationToken cancellationToken = default)
        {
            await gate.WaitIfArmedAsync(cancellationToken);
            return await inner.CreateGenerationAsync(ownerId, generationId, value, cancellationToken);
        }

        public Task<SecretPayload> ResolveGenerationAsync(string name, string ownerId, string generationId, CancellationToken cancellationToken = default) =>
            inner.ResolveGenerationAsync(name, ownerId, generationId, cancellationToken);

        public Task<bool> DeleteGenerationAsync(string name, string ownerId, string generationId, CancellationToken cancellationToken = default) =>
            inner.DeleteGenerationAsync(name, ownerId, generationId, cancellationToken);
    }

    private sealed class BlockingManagedSecretManager(
        DefaultSecretManager inner,
        ConnectGenerationGate? createGate,
        ConnectGenerationGate? resolveGate) : IManagedSecretManager
    {
        public async Task<Secret> CreateGenerationAsync(string ownerId, string generationId, string value, CancellationToken cancellationToken = default)
        {
            if (createGate != null)
            {
                createGate.SignalEntered();
                await createGate.WaitForRelease.WaitAsync(cancellationToken);
            }

            return await inner.CreateGenerationAsync(ownerId, generationId, value, cancellationToken);
        }

        public async Task<SecretPayload> ResolveGenerationAsync(string name, string ownerId, string generationId, CancellationToken cancellationToken = default)
        {
            if (resolveGate != null)
            {
                resolveGate.SignalEntered();
                await resolveGate.WaitForRelease.WaitAsync(cancellationToken);
            }

            return await inner.ResolveGenerationAsync(name, ownerId, generationId, cancellationToken);
        }

        public Task<bool> DeleteGenerationAsync(string name, string ownerId, string generationId, CancellationToken cancellationToken = default) =>
            inner.DeleteGenerationAsync(name, ownerId, generationId, cancellationToken);
    }
}
