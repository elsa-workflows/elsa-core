using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.PerformanceTests.ExternalAuthentication;

/// <summary>
/// Bounded scenarios for the performance goals in the External Authentication feature plan:
/// Login Method discovery, management listing over 10,000 connections, and broker initiation overhead.
/// Provider network latency is deliberately excluded.
/// </summary>
[Config(typeof(Config))]
public class ExternalAuthenticationBenchmarks
{
    private const string TenantId = "tenant-a";
    private DefaultIdentityProviderConnectionRegistry _discoveryRegistry = null!;
    private InMemoryIdentityProviderConnectionStore _managementStore = null!;
    private ExternalAuthenticationBroker _broker = null!;
    private HmacExternalAuthenticationHandleHasher _hasher = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var clock = new FixedClock();
        var calculator = new ConnectionRevisionCalculator();
        var discoveryConnections = Enumerable.Range(0, 50).Select(index => Connection($"discovery-{index:D2}", $"provider-{index:D2}", index)).ToArray();
        _discoveryRegistry = new DefaultIdentityProviderConnectionRegistry([new StaticConnectionSource(discoveryConnections)], calculator);

        _managementStore = new InMemoryIdentityProviderConnectionStore();
        foreach (var index in Enumerable.Range(0, 10_000))
            await _managementStore.CreateAsync(Connection($"management-{index:D5}", $"provider-{index:D5}", index % 50));

        _hasher = new HmacExternalAuthenticationHandleHasher();
        var adapter = new BenchmarkAdapter();
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions
        {
            Clients =
            [
                new AuthenticationClient(
                    "studio", "Studio", AuthenticationClientType.Public,
                    new HashSet<Uri> { new("https://studio.example/authentication/external/callback") },
                    new HashSet<Uri>(),
                    new HashSet<string> { "https://studio.example" },
                    new HashSet<string> { "/" },
                    null,
                    true)
            ]
        });
        var unused = new UnusedBrokerDependencies();
        _broker = new ExternalAuthenticationBroker(
            _discoveryRegistry,
            [adapter],
            [],
            _hasher,
            new EphemeralDataProtectionProvider(),
            unused,
            unused,
            new InMemoryExternalAuthenticationStateStore(clock),
            new InMemoryAuthorizationGrantStore(clock),
            new InMemoryExternalAuthenticationSessionStore(clock),
            unused,
            unused,
            unused,
            unused,
            unused,
            clock,
            options);
    }

    [Benchmark(Description = "Discover 50 effective login methods")]
    public ValueTask<EffectiveConnectionRegistry> Discovery() => _discoveryRegistry.GetAsync(TenantId);

    [Benchmark(Description = "Build a 100-row management page from 10,000 connections")]
    public async Task<IReadOnlyCollection<IdentityProviderConnection>> ManagementPaging()
    {
        var matches = await _managementStore.FindAsync(new ConnectionFilter { Scope = new ConnectionScope(ConnectionScopeKind.Tenant, TenantId) });
        return matches.Items.Take(100).ToArray();
    }

    [Benchmark(Description = "Broker external initiation excluding provider latency")]
    public ValueTask<BrokerInitiationResult> BrokerInitiation() => _broker.InitiateExternalAsync(
        new BrokerAuthorizationRequest(
            "studio",
            new Uri("https://studio.example/authentication/external/callback"),
            "code",
            "benchmark-pkce-challenge",
            "S256",
            "/",
            "provider-00"),
        TenantId);

    [GlobalCleanup]
    public void GlobalCleanup() => _hasher.Dispose();

    private static IdentityProviderConnection Connection(string id, string key, int order) => new()
    {
        Id = id,
        TenantId = TenantId,
        Key = key,
        AdapterType = BenchmarkAdapter.AdapterType,
        AdapterSettingsVersion = 1,
        AdapterSettings = JsonSerializer.SerializeToElement(new { }),
        DisplayName = key,
        DisplayOrder = order,
        IsEnabled = true,
        MaterialRevision = $"revision-{id}"
    };

    private sealed class StaticConnectionSource(IReadOnlyCollection<IdentityProviderConnection> connections) : IIdentityProviderConnectionSource
    {
        public string Name => "benchmark";
        public ConnectionSourceOwnership Ownership => ConnectionSourceOwnership.Configuration;
        public ValueTask<ConnectionSourceSnapshot> GetSnapshotAsync(ConnectionScope scope, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ConnectionSourceSnapshot(scope, "v1", scope.TenantId == TenantId ? connections : []));
    }

    private sealed class BenchmarkAdapter : IExternalAuthenticationAdapter
    {
        public const string AdapterType = "benchmark";
        public string Type => AdapterType;
        public ExternalAuthenticationAdapterDescriptor Describe() => new(AdapterType, "Benchmark", "Benchmark adapter", 1, [], new ExternalAuthenticationAdapterCapabilities(true, true, false), null);
        public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default) => ValueTask.FromResult(new ConnectionValidationResult(true, [], []));
        public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default) => ValueTask.FromResult(new ExternalAuthorizationRequest(new Uri($"https://issuer.example/authorize?state={context.CorrelationState}"), []));
        public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnusedBrokerDependencies :
        IExternalIdentityResolver,
        IPermissionGrantResolver,
        IExternalAuthenticationTokenIssuer,
        IUserCredentialsValidator,
        IUserProvider,
        IRoleProvider,
        IElsaTokenService
    {
        public ValueTask<ExternalIdentityResolution> ResolveAsync(ExternalIdentityResolutionContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<PermissionGrantResult> ResolveAsync(PermissionGrantResolutionContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalTokenResponse> IssueAsync(ExternalAuthenticationSession session, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalTokenResponse> RefreshAsync(string clientId, SensitiveString refreshToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<User?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<IssuedAccessToken> IssueAccessTokenAsync(TokenIssuanceContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<IssuedAccessToken> IssueRefreshTokenAsync(TokenIssuanceContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 7, 24, 0, 0, 0, TimeSpan.Zero);
    }
}
