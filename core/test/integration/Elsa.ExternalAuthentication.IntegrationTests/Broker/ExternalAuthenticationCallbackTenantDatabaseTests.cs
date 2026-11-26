using System.Text.Json;
using Elsa.Authorization;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Broker;

public class ExternalAuthenticationCallbackTenantDatabaseTests
{
    [Fact(DisplayName = "A callback with no tenant header writes to the tenant database, not the host database")]
    public async Task CompleteCallbackAsync_WhenTenantHeaderIsMissing_UsesTheTenantDatabase()
    {
        var accessor = new DefaultTenantAccessor();
        var acme = TenantWithDatabase("acme", "Tenant-Acme-Database");
        var log = new AmbientDatabaseLog(accessor);
        var stateStore = new RecordingStateStore(new InMemoryExternalAuthenticationStateStore(new TestClock()), log);
        var sessionStore = new RecordingSessionStore(new InMemoryExternalAuthenticationSessionStore(new TestClock()), log);
        var identityResolver = Substitute.For<IExternalIdentityResolver>();
        identityResolver.ResolveAsync(Arg.Any<ExternalIdentityResolutionContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                log.Record("resolve");
                return ValueTask.FromResult(new ExternalIdentityResolution("user-a", true));
            });
        var adapter = new BrokerSecurityTests.RecordingAdapter
        {
            AuthenticationResult = new ExternalAuthenticationResult(new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims), EmptyClaims, [])
        };
        var broker = CreateBroker(adapter, accessor, acme, stateStore, sessionStore, identityResolver);

        using (accessor.PushContext(acme))
            await broker.InitiateExternalAsync(Request(), "acme");

        Assert.Null(accessor.Tenant);
        Assert.Contains("Tenant-Acme-Database", log.OnPut);
        Assert.DoesNotContain("Host-Database", log.OnPut);

        var result = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, Parameters(adapter));

        Assert.Null(result.Error);
        Assert.Equal(["Tenant-Acme-Database"], log.OnTake);
        Assert.Contains("Tenant-Acme-Database", log.OnResolve);
        Assert.Contains("Tenant-Acme-Database", log.OnSessionSave);
        Assert.DoesNotContain("Host-Database", log.All);
        Assert.DoesNotContain("<none>", log.OnTake);
        Assert.Null(accessor.Tenant);
    }

    [Fact(DisplayName = "A tampered callback state is rejected and does not touch the host or tenant store")]
    public async Task CompleteCallbackAsync_WhenStateIsTampered_FailsClosedWithoutStoreAccess()
    {
        var accessor = new DefaultTenantAccessor();
        var acme = TenantWithDatabase("acme", "Tenant-Acme-Database");
        var log = new AmbientDatabaseLog(accessor);
        var stateStore = new RecordingStateStore(new InMemoryExternalAuthenticationStateStore(new TestClock()), log);
        var adapter = new BrokerSecurityTests.RecordingAdapter
        {
            AuthenticationResult = new ExternalAuthenticationResult(new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims), EmptyClaims, [])
        };
        var broker = CreateBroker(adapter, accessor, acme, stateStore, new InMemoryExternalAuthenticationSessionStore(new TestClock()), Substitute.For<IExternalIdentityResolver>());

        using (accessor.PushContext(acme))
            await broker.InitiateExternalAsync(Request(), "acme");
        log.Clear();

        var state = adapter.CorrelationState!;
        var tampered = state[..^2] + (state[^1] == 'A' ? "B" : "A");
        var result = await broker.CompleteCallbackAsync("contoso", tampered, Parameters(adapter, tampered));

        Assert.Equal("invalid_request", result.Error?.Error);
        Assert.Empty(log.OnTake);
        Assert.DoesNotContain("Host-Database", log.All);
        Assert.Null(accessor.Tenant);
    }

    [Fact(DisplayName = "A tenant mismatch between the state and the stored transaction is rejected")]
    public async Task CompleteCallbackAsync_WhenStoredTenantIdDoesNotMatchState_FailsClosed()
    {
        var accessor = new DefaultTenantAccessor();
        var acme = TenantWithDatabase("acme", "Tenant-Acme-Database");
        var log = new AmbientDatabaseLog(accessor);
        var stateStore = new RecordingStateStore(new InMemoryExternalAuthenticationStateStore(new TestClock()), log);
        var identityResolver = Substitute.For<IExternalIdentityResolver>();
        identityResolver.ResolveAsync(Arg.Any<ExternalIdentityResolutionContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new ExternalIdentityResolution("user-a", true)));
        var adapter = new BrokerSecurityTests.RecordingAdapter
        {
            AuthenticationResult = new ExternalAuthenticationResult(new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims), EmptyClaims, [])
        };
        var broker = CreateBroker(adapter, accessor, acme, stateStore, new InMemoryExternalAuthenticationSessionStore(new TestClock()), identityResolver);

        using (accessor.PushContext(acme))
            await broker.InitiateExternalAsync(Request(), "acme");

        // The in-memory store keeps the same BrokerTransaction instance RecordingAdapter captured on PutAsync.
        adapter.Transaction!.TenantId = "other-tenant";
        var result = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, Parameters(adapter));

        Assert.Equal("invalid_request", result.Error?.Error);
        await identityResolver.DidNotReceive().ResolveAsync(Arg.Any<ExternalIdentityResolutionContext>(), Arg.Any<CancellationToken>());
        Assert.Equal(["Tenant-Acme-Database"], log.OnTake);
        Assert.Empty(log.OnResolve);
    }

    [Fact(DisplayName = "The default single-tenant callback restores the default tenant explicitly")]
    public async Task CompleteCallbackAsync_WhenTenantIdIsDefault_UsesTheDefaultTenantDatabase()
    {
        var accessor = new DefaultTenantAccessor();
        var defaultTenant = new Tenant
        {
            Id = Tenant.DefaultTenantId,
            Name = "Default",
            Configuration = Configuration("Default-Database")
        };
        var log = new AmbientDatabaseLog(accessor);
        var stateStore = new RecordingStateStore(new InMemoryExternalAuthenticationStateStore(new TestClock()), log);
        var sessionStore = new RecordingSessionStore(new InMemoryExternalAuthenticationSessionStore(new TestClock()), log);
        var identityResolver = Substitute.For<IExternalIdentityResolver>();
        identityResolver.ResolveAsync(Arg.Any<ExternalIdentityResolutionContext>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                log.Record("resolve");
                return ValueTask.FromResult(new ExternalIdentityResolution("user-a", true));
            });
        var adapter = new BrokerSecurityTests.RecordingAdapter
        {
            AuthenticationResult = new ExternalAuthenticationResult(new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims), EmptyClaims, [])
        };
        var broker = CreateBroker(adapter, accessor, defaultTenant, stateStore, sessionStore, identityResolver);

        using (accessor.PushContext(defaultTenant))
            await broker.InitiateExternalAsync(Request(), Tenant.DefaultTenantId);

        var result = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, Parameters(adapter));

        Assert.Null(result.Error);
        Assert.Equal(["Default-Database"], log.OnTake);
        Assert.Contains("Default-Database", log.OnResolve);
        Assert.Contains("Default-Database", log.OnSessionSave);
        Assert.DoesNotContain("<none>", log.OnTake);
    }

    [Fact(DisplayName = "A successful callback consumes the nonce once")]
    public async Task CompleteCallbackAsync_WhenReplayed_RejectsTheSecondAttempt()
    {
        var accessor = new DefaultTenantAccessor();
        var acme = TenantWithDatabase("acme", "Tenant-Acme-Database");
        var adapter = new BrokerSecurityTests.RecordingAdapter
        {
            AuthenticationResult = new ExternalAuthenticationResult(new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims), EmptyClaims, [])
        };
        var identityResolver = Substitute.For<IExternalIdentityResolver>();
        identityResolver.ResolveAsync(Arg.Any<ExternalIdentityResolutionContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new ExternalIdentityResolution("user-a", true)));
        var broker = CreateBroker(adapter, accessor, acme, new InMemoryExternalAuthenticationStateStore(new TestClock()), new InMemoryExternalAuthenticationSessionStore(new TestClock()), identityResolver);

        using (accessor.PushContext(acme))
            await broker.InitiateExternalAsync(Request(), "acme");

        var first = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, Parameters(adapter));
        var replay = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, Parameters(adapter));

        Assert.Null(first.Error);
        Assert.Equal("invalid_request", replay.Error?.Error);
    }

    [Fact(DisplayName = "An expired transaction returns flow_expired, not invalid_request")]
    public async Task CompleteCallbackAsync_WhenTransactionHasExpired_ReturnsFlowExpired()
    {
        var accessor = new DefaultTenantAccessor();
        var acme = TenantWithDatabase("acme", "Tenant-Acme-Database");
        var clock = new MutableClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var adapter = new BrokerSecurityTests.RecordingAdapter
        {
            AuthenticationResult = new ExternalAuthenticationResult(new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims), EmptyClaims, [])
        };
        var identityResolver = Substitute.For<IExternalIdentityResolver>();
        var broker = CreateBroker(adapter, accessor, acme, new InMemoryExternalAuthenticationStateStore(clock), new InMemoryExternalAuthenticationSessionStore(clock), identityResolver, clock);

        using (accessor.PushContext(acme))
            await broker.InitiateExternalAsync(Request(), "acme");

        clock.UtcNow = adapter.Transaction!.ExpiresAt.AddSeconds(1);

        var result = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, Parameters(adapter));

        Assert.Equal("flow_expired", result.Error?.Error);
        await identityResolver.DidNotReceive().ResolveAsync(Arg.Any<ExternalIdentityResolutionContext>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A logout callback with no tenant header writes to the tenant database, not the host database")]
    public async Task CompleteLogoutAsync_WhenTenantHeaderIsMissing_UsesTheTenantDatabase()
    {
        var accessor = new DefaultTenantAccessor();
        var acme = TenantWithDatabase("acme", "Tenant-Acme-Database");
        var log = new AmbientDatabaseLog(accessor);
        var clock = new TestClock();
        var stateStore = new RecordingStateStore(new InMemoryExternalAuthenticationStateStore(clock), log);
        var sessionStore = new InMemoryExternalAuthenticationSessionStore(clock);
        var adapter = new BrokerSecurityTests.RecordingAdapter();
        var broker = CreateBroker(
            adapter,
            accessor,
            acme,
            stateStore,
            sessionStore,
            Substitute.For<IExternalIdentityResolver>(),
            clock,
            clients: [StudioClientWithLogout()]);

        await sessionStore.SaveAsync(Session("session-a", acme.Id, clock.UtcNow));
        var logoutUri = new Uri("https://studio.example/authentication/external/logout-callback");
        using (accessor.PushContext(acme))
        {
            var initiated = await broker.LogoutAsync(new BrokerLogoutRequest("studio", logoutUri, "upstream"), "session-a");
            Assert.Null(initiated.Error);
        }

        log.Clear();
        Assert.Null(accessor.Tenant);

        var result = await broker.CompleteLogoutAsync("contoso", adapter.CorrelationState!);

        Assert.Null(result.Error);
        Assert.Equal(["Tenant-Acme-Database"], log.OnTake);
        Assert.DoesNotContain("Host-Database", log.All);
        Assert.DoesNotContain("<none>", log.OnTake);
        Assert.Null(accessor.Tenant);
    }

    [Fact(DisplayName = "A preview callback with no tenant header writes to the tenant database, not the host database")]
    public async Task CompletePreviewAsync_WhenTenantHeaderIsMissing_UsesTheTenantDatabase()
    {
        var accessor = new DefaultTenantAccessor();
        var acme = TenantWithDatabase("acme", "Tenant-Acme-Database");
        var log = new AmbientDatabaseLog(accessor);
        var clock = new TestClock();
        var stateStore = new RecordingStateStore(new InMemoryExternalAuthenticationStateStore(clock), log);
        var adapter = new BrokerSecurityTests.RecordingAdapter
        {
            AuthenticationResult = new ExternalAuthenticationResult(new ExternalIdentity("https://issuer.example", "subject-a", EmptyClaims), EmptyClaims, [])
        };
        var hasher = new HmacExternalAuthenticationHandleHasher();
        var previews = CreatePreviewService(adapter, accessor, acme, stateStore, hasher, clock);
        const string handle = "preview-handle";
        var expiresAt = clock.UtcNow.Add(Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions()).Value.Lifetimes.PreviewLifetime);
        var start = new BrokerTransaction
        {
            HandleHash = hasher.Hash(handle),
            Purpose = BrokerTransactionPurpose.Preview,
            ClientId = "administrator-a",
            CallbackUri = new Uri($"/external-authentication/previews/{handle}/authorize", UriKind.Relative),
            ReturnPath = "/",
            TenantId = acme.Id,
            ConnectionId = "connection-a",
            ConnectionMaterialRevision = "revision-a",
            PkceChallenge = string.Empty,
            ExpiresAt = expiresAt
        };

        using (accessor.PushContext(acme))
        {
            await stateStore.PutAsync("PreviewStart", start.HandleHash, start, start.ExpiresAt);
            var authorized = await previews.AuthorizeAsync(handle);
            Assert.IsType<PreviewAuthorizeResult.Redirect>(authorized);
        }

        log.Clear();
        Assert.Null(accessor.Tenant);

        var result = await previews.CompleteAsync("connection-a", adapter.CorrelationState!, Parameters(adapter));

        Assert.IsType<PreviewCallbackResult.Completed>(result);
        Assert.Equal(["Tenant-Acme-Database"], log.OnTake);
        Assert.DoesNotContain("Host-Database", log.All);
        Assert.DoesNotContain("<none>", log.OnTake);
        Assert.Null(accessor.Tenant);
    }

    private static ExternalAuthenticationBroker CreateBroker(
        BrokerSecurityTests.RecordingAdapter adapter,
        ITenantAccessor accessor,
        Tenant tenant,
        IExternalAuthenticationStateStore stateStore,
        IExternalAuthenticationSessionStore sessionStore,
        IExternalIdentityResolver identityResolver,
        ISystemClock? clock = null,
        IReadOnlyCollection<AuthenticationClient>? clients = null)
    {
        var permissionGrantResolver = Substitute.For<IPermissionGrantResolver>();
        permissionGrantResolver.ResolveAsync(Arg.Any<PermissionGrantResolutionContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PermissionGrantResult([], [])));
        return BrokerSecurityTests.CreateBroker(
            adapter,
            identityResolver: identityResolver,
            permissionGrantResolver: permissionGrantResolver,
            sessionStore: sessionStore,
            tenantAccessor: accessor,
            stateStore: stateStore,
            tenantFinder: new MapTenantFinder(tenant),
            configureConnection: connection =>
            {
                connection.TenantId = tenant.Id;
                connection.UpstreamLogoutMode = UpstreamLogoutMode.UserChoice;
            },
            clock: clock,
            clients: clients);
    }

    private static PreviewSignInService CreatePreviewService(
        BrokerSecurityTests.RecordingAdapter adapter,
        ITenantAccessor accessor,
        Tenant tenant,
        IExternalAuthenticationStateStore stateStore,
        IExternalAuthenticationHandleHasher hasher,
        ISystemClock clock)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions());
        var connection = new IdentityProviderConnection
        {
            Id = "connection-a",
            TenantId = tenant.Id,
            Key = "contoso",
            AdapterType = adapter.Type,
            AdapterSettings = JsonSerializer.SerializeToElement(new { }),
            DisplayName = "Contoso",
            IsEnabled = true,
            MaterialRevision = "revision-a"
        };
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, ConnectionScope.Host, ConnectionValidity.Valid, false, "test");
        var registry = Substitute.For<IIdentityProviderConnectionRegistry>();
        registry.FindByIdAsync(tenant.Id, connection.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effective));
        var validityAssessor = Substitute.For<IIdentityProviderConnectionValidityAssessor>();
        validityAssessor.AssessAsync(Arg.Any<EffectiveIdentityProviderConnection>(), Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<EffectiveIdentityProviderConnection>()));
        var adapters = new SingleAdapterRegistry(adapter);
        var management = new IdentityProviderConnectionManagementService(
            null!,
            registry,
            validityAssessor,
            null!,
            adapters,
            Substitute.For<IAdapterSettingsMigrationService>(),
            null!,
            null!,
            null!,
            [],
            [],
            null!,
            new PermissionEvaluator(),
            null!,
            clock,
            options,
            null!,
            null!,
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<IdentityProviderConnectionManagementService>.Instance);
        var dataProtection = new EphemeralDataProtectionProvider();
        return new PreviewSignInService(
            management,
            adapters,
            [],
            [],
            Substitute.For<IExternalIdentityProvisioner>(),
            Substitute.For<IPermissionGrantResolver>(),
            stateStore,
            new InMemoryPreviewResultStore(clock),
            hasher,
            dataProtection,
            new ExternalAuthenticationCallbackTenant(dataProtection, new MapTenantFinder(tenant), accessor),
            clock,
            options,
            new ExternalAuthenticationSecurityNotifier(new ServiceCollection().BuildServiceProvider()));
    }

    private static AuthenticationClient StudioClientWithLogout() =>
        new("studio", "Studio", AuthenticationClientType.Public,
            new HashSet<Uri> { new("https://studio.example/authentication/external/callback") },
            new HashSet<Uri> { new("https://studio.example/authentication/external/logout-callback") },
            new HashSet<string> { "https://studio.example" },
            new HashSet<string> { "/workflows" }, null, true);

    private static ExternalAuthenticationSession Session(string id, string tenantId, DateTimeOffset now) => new()
    {
        Id = id,
        AuthenticationClientId = "studio",
        TenantId = tenantId,
        UserId = "user-a",
        ConnectionKey = "contoso",
        ConnectionMaterialRevision = "revision-a",
        Issuer = "https://issuer.example",
        SubjectHash = "subject",
        StartedAt = now,
        LastRefreshedAt = now,
        ExpiresAt = now.AddHours(1),
        RefreshExpiresAt = now.AddHours(1)
    };

    private static BrokerAuthorizationRequest Request() =>
        new("studio", new Uri("https://studio.example/authentication/external/callback"), "code", "challenge", "S256", "/workflows", "contoso");

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Parameters(BrokerSecurityTests.RecordingAdapter adapter, string? state = null) =>
        new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [state ?? adapter.CorrelationState!] };

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> EmptyClaims { get; } = new Dictionary<string, IReadOnlyCollection<string>>();

    private static Tenant TenantWithDatabase(string id, string database) => new()
    {
        Id = id,
        Name = id,
        Configuration = Configuration(database)
    };

    private static IConfiguration Configuration(string database) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SqlServer"] = database
        }).Build();

    private sealed class TestClock : ISystemClock { public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-01-01T00:00:00Z"); }

    private sealed class MutableClock(DateTimeOffset utcNow) : ISystemClock { public DateTimeOffset UtcNow { get; set; } = utcNow; }

    private sealed class SingleAdapterRegistry(IExternalAuthenticationAdapter adapter) : IExternalAuthenticationAdapterRegistry
    {
        public IReadOnlyCollection<ExternalAuthenticationAdapterDescriptor> ListDescriptors() => [];
        public bool TryGet(string type, out IExternalAuthenticationAdapter resolved)
        {
            resolved = adapter;
            return string.Equals(type, adapter.Type, StringComparison.Ordinal);
        }
    }

    private sealed class MapTenantFinder(params Tenant[] tenants) : ITenantFinder
    {
        public Task<Tenant?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            tenantId = string.IsNullOrEmpty(tenantId) ? Tenant.DefaultTenantId : tenantId;
            return Task.FromResult(tenants.FirstOrDefault(candidate => candidate.Id == tenantId));
        }
    }

    private sealed class AmbientDatabaseLog(ITenantAccessor accessor)
    {
        public List<string> OnPut { get; } = [];
        public List<string> OnTake { get; } = [];
        public List<string> OnResolve { get; } = [];
        public List<string> OnSessionSave { get; } = [];
        public IEnumerable<string> All => OnPut.Concat(OnTake).Concat(OnResolve).Concat(OnSessionSave);

        public void Record(string operation)
        {
            var current = accessor.Tenant is null
                ? "<none>"
                : accessor.Tenant.Configuration.GetConnectionString("SqlServer") ?? $"id:{accessor.Tenant.Id}";
            (operation switch
            {
                "put" => OnPut,
                "take" => OnTake,
                "resolve" => OnResolve,
                "session" => OnSessionSave,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            }).Add(current);
        }

        public void Clear()
        {
            OnPut.Clear();
            OnTake.Clear();
            OnResolve.Clear();
            OnSessionSave.Clear();
        }
    }

    private sealed class RecordingStateStore(IExternalAuthenticationStateStore inner, AmbientDatabaseLog log) : IExternalAuthenticationStateStore
    {
        public ValueTask PutAsync<T>(string purpose, string handleHash, T value, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            log.Record("put");
            return inner.PutAsync(purpose, handleHash, value, expiresAt, cancellationToken);
        }

        public ValueTask<TakeResult<T>> TryTakeAsync<T>(string purpose, string handleHash, CancellationToken cancellationToken = default)
        {
            log.Record("take");
            return inner.TryTakeAsync<T>(purpose, handleHash, cancellationToken);
        }
    }

    private sealed class RecordingSessionStore(IExternalAuthenticationSessionStore inner, AmbientDatabaseLog log) : IExternalAuthenticationSessionStore
    {
        public ValueTask SaveAsync(ExternalAuthenticationSession session, CancellationToken cancellationToken = default)
        {
            log.Record("session");
            return inner.SaveAsync(session, cancellationToken);
        }

        public ValueTask<IReadOnlyCollection<ExternalAuthenticationSession>> FindAsync(ExternalAuthenticationSessionFilter filter, CancellationToken cancellationToken = default) => inner.FindAsync(filter, cancellationToken);
        public ValueTask<ExternalAuthenticationSession?> FindByIdAsync(string sessionId, CancellationToken cancellationToken = default) => inner.FindByIdAsync(sessionId, cancellationToken);
        public ValueTask<ExternalAuthenticationSession?> FindByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken = default) => inner.FindByRefreshTokenHashAsync(refreshTokenHash, cancellationToken);
        public ValueTask<bool> RevokeAsync(string sessionId, string reason, DateTimeOffset revokedAt, CancellationToken cancellationToken = default) => inner.RevokeAsync(sessionId, reason, revokedAt, cancellationToken);
        public ValueTask<int> RevokeActiveForConnectionAsync(string connectionKey, string reason, DateTimeOffset revokedAt, CancellationToken cancellationToken = default) => inner.RevokeActiveForConnectionAsync(connectionKey, reason, revokedAt, cancellationToken);
        public ValueTask<ExternalAuthenticationSessionRotationResult> TryRotateRefreshTokenAsync(string sessionId, string refreshTokenHash, long expectedGeneration, string nextRefreshTokenHash, DateTimeOffset refreshedAt, CancellationToken cancellationToken = default) =>
            inner.TryRotateRefreshTokenAsync(sessionId, refreshTokenHash, expectedGeneration, nextRefreshTokenHash, refreshedAt, cancellationToken);
    }
}
