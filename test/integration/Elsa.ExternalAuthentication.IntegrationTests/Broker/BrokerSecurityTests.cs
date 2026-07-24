using System.Text.Json;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Broker;

public class BrokerSecurityTests
{
    [Fact]
    public async Task ExternalInitiationUsesExactlyOneOpaqueProviderStateAndPersistsAdapterPayload()
    {
        var adapter = new RecordingAdapter();
        var broker = CreateBroker(adapter);
        var request = Request("/workflows");

        var result = await broker.InitiateExternalAsync(request, "tenant-a");

        Assert.Null(result.Error);
        Assert.NotNull(result.NavigationUri);
        Assert.Equal(adapter.CorrelationState, Query(result.NavigationUri!, "state"));
        Assert.NotEqual(adapter.Transaction!.HandleHash, adapter.CorrelationState);
        Assert.NotEqual([1, 2, 3], adapter.Transaction.ProtectedPayload);
    }

    [Theory]
    [InlineData("//evil.example")]
    [InlineData("/administration")]
    public async Task InitiationRejectsReturnPathsOutsideTheAuthenticationClientAllowlist(string returnPath)
    {
        var adapter = new RecordingAdapter();
        var broker = CreateBroker(adapter);

        var result = await broker.InitiateExternalAsync(Request(returnPath), "tenant-a");

        Assert.Equal("invalid_request", result.Error?.Error);
        Assert.Null(adapter.CorrelationState);
    }

    [Fact]
    public async Task CallbackFailureAfterTrustedStateRedirectsOnlyToRegisteredCallback()
    {
        var adapter = new RecordingAdapter { ThrowOnCallback = true };
        var broker = CreateBroker(adapter);
        var initiated = await broker.InitiateExternalAsync(Request("/workflows"), "tenant-a");

        var result = await broker.CompleteCallbackAsync("connection-a", adapter.CorrelationState!, new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] });

        Assert.Equal("authentication_failed", result.Error?.Error);
        Assert.StartsWith("https://studio.example/authentication/external/callback?", result.RedirectUri?.AbsoluteUri);
        Assert.DoesNotContain("issuer.example", result.RedirectUri?.AbsoluteUri);
    }

    [Fact]
    public async Task ProviderCallbackStateCannotBeReplayed()
    {
        var adapter = new RecordingAdapter { ThrowOnCallback = true };
        var broker = CreateBroker(adapter);
        await broker.InitiateExternalAsync(Request("/workflows"), "tenant-a");
        var parameters = new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] };

        _ = await broker.CompleteCallbackAsync("connection-a", adapter.CorrelationState!, parameters);
        var replay = await broker.CompleteCallbackAsync("connection-a", adapter.CorrelationState!, parameters);

        Assert.Equal("invalid_request", replay.Error?.Error);
        Assert.Null(replay.RedirectUri);
    }

    [Theory]
    [InlineData("revision")]
    [InlineData("disabled")]
    [InlineData("archived")]
    public async Task CallbackRejectsConnectionChangesAfterInitiation(string change)
    {
        var adapter = new RecordingAdapter { ThrowOnCallback = true };
        var broker = CreateBroker(adapter);
        await broker.InitiateExternalAsync(Request("/workflows"), "tenant-a");
        var connection = adapter.Connection!.Connection;
        switch (change)
        {
            case "revision": connection.MaterialRevision = "revision-b"; break;
            case "disabled": connection.IsEnabled = false; break;
            case "archived": connection.ArchivedAt = DateTimeOffset.UtcNow; break;
        }

        var result = await broker.CompleteCallbackAsync("connection-a", adapter.CorrelationState!, new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] });

        Assert.Equal(change == "revision" ? "flow_changed" : "method_unavailable", result.Error?.Error);
        Assert.StartsWith("https://studio.example/authentication/external/callback?", result.RedirectUri?.AbsoluteUri);
    }

    [Fact]
    public async Task RefreshRotationRevokesTheSessionWhenAnOlderTokenIsReused()
    {
        var clock = new TestClock();
        var store = new InMemoryExternalAuthenticationSessionStore(clock);
        var connection = new IdentityProviderConnection { Id = "connection-a", TenantId = "tenant-a", Key = "contoso", AdapterType = "fake", DisplayName = "Contoso", IsEnabled = true, MaterialRevision = "revision-a" };
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, new(ConnectionScopeKind.Tenant, "tenant-a"), ConnectionValidity.Valid, false, "test");
        var registry = Substitute.For<IIdentityProviderConnectionRegistry>();
        registry.FindByIdAsync("tenant-a", "connection-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effective));
        var users = Substitute.For<IUserProvider>();
        users.FindAsync(Arg.Any<UserFilter>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" }));
        var roles = Substitute.For<IRoleProvider>();
        roles.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<IEnumerable<Role>>([]));
        var tokens = Substitute.For<IElsaTokenService>();
        tokens.IssueAccessTokenAsync(Arg.Any<TokenIssuanceContext>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(new IssuedAccessToken("access", clock.UtcNow.AddHours(1))));
        var issuer = new DefaultExternalAuthenticationTokenIssuer(store, registry, [], users, roles, tokens, clock);
        var session = new ExternalAuthenticationSession { Id = "session-a", AuthenticationClientId = "studio", TenantId = "tenant-a", UserId = "user-a", ConnectionId = "connection-a", ConnectionMaterialRevision = "revision-a", SecretGenerationFingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData([])), Issuer = "issuer", SubjectHash = "subject", StartedAt = clock.UtcNow, LastRefreshedAt = clock.UtcNow, ExpiresAt = clock.UtcNow.AddHours(1), RefreshExpiresAt = clock.UtcNow.AddHours(1) };

        var first = await issuer.IssueAsync(session);
        var second = await issuer.RefreshAsync("studio", new SensitiveString(first.RefreshToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => issuer.RefreshAsync("studio", new SensitiveString(first.RefreshToken)).AsTask());
        var revoked = await store.FindByIdAsync(session.Id);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.NotNull(revoked?.RevokedAt);
        Assert.Equal("refresh_token_reuse", revoked?.RevocationReason);
    }

    [Fact]
    public async Task PkceMismatchConsumesTheAuthorizationCode()
    {
        var grants = new InMemoryAuthorizationGrantStore(new TestClock());
        var broker = CreateBroker(new RecordingAdapter(), grants, new FixedHasher());
        await grants.SaveAsync(new AuthorizationGrant { CodeHash = "hash:code", ClientId = "studio", CallbackUri = new Uri("https://studio.example/authentication/external/callback"), TenantId = "tenant-a", UserId = "user-a", PkceChallenge = "expected", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1) });

        var result = await broker.ExchangeAsync(new BrokerTokenRequest("authorization_code", "studio", new Uri("https://studio.example/authentication/external/callback"), "code", "wrong", null, "https://studio.example"));
        var after = await grants.TryTakeAsync("hash:code");

        Assert.Equal("invalid_request", result.Error?.Error);
        Assert.IsType<TakeResult<AuthorizationGrant>.AlreadyConsumed>(after);
    }

    [Fact]
    public async Task InitiationRejectsAnExactCallbackUriMismatch()
    {
        var adapter = new RecordingAdapter();
        var broker = CreateBroker(adapter);

        var result = await broker.InitiateExternalAsync(new BrokerAuthorizationRequest("studio", new Uri("https://studio.example/other"), "code", "challenge", "S256", "/workflows", "contoso"), "tenant-a");

        Assert.Equal("invalid_request", result.Error?.Error);
        Assert.Null(adapter.CorrelationState);
    }

    [Fact]
    public async Task ExchangeRequiresAnExactPublicOriginAndConfidentialBasicClientId()
    {
        var publicBroker = CreateBroker(new RecordingAdapter());
        var publicResult = await publicBroker.ExchangeAsync(new BrokerTokenRequest("authorization_code", "studio", new Uri("https://studio.example/authentication/external/callback"), "code", "verifier", null, "https://studio.example.attacker"));

        var secretResolver = new MutableSecretResolver();
        var confidentialClient = new AuthenticationClient("confidential", "Confidential", AuthenticationClientType.Confidential, new HashSet<Uri> { new("https://studio.example/authentication/external/callback") }, new HashSet<Uri>(), new HashSet<string>(), new HashSet<string> { "/workflows" }, new SecretBinding("test", "client"), true);
        var confidentialBroker = CreateBroker(new RecordingAdapter(), clients: [confidentialClient], resolvers: [secretResolver]);
        var confidentialResult = await confidentialBroker.ExchangeAsync(new BrokerTokenRequest("authorization_code", "confidential", new Uri("https://studio.example/authentication/external/callback"), "code", "verifier", null, null, "other-client", "secret"));

        Assert.Equal("invalid_request", publicResult.Error?.Error);
        Assert.Equal("invalid_request", confidentialResult.Error?.Error);
    }

    [Fact]
    public async Task SecretGenerationRotationInvalidatesTrustedCallback()
    {
        var resolver = new MutableSecretResolver();
        var adapter = new RecordingAdapter { ThrowOnCallback = true };
        var broker = CreateBroker(adapter, resolvers: [resolver], configureConnection: connection => connection.SecretBindings["clientSecret"] = new SecretBinding("test", "client"));
        await broker.InitiateExternalAsync(Request("/workflows"), "tenant-a");
        resolver.Generation = "generation-2";

        var result = await broker.CompleteCallbackAsync("connection-a", adapter.CorrelationState!, new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] });

        Assert.Equal("flow_changed", result.Error?.Error);
    }

    [Fact]
    public async Task RejectedBrokerOutcomesPublishTheirSafePublicCategory()
    {
        var sender = Substitute.For<INotificationSender>();
        await using var services = new ServiceCollection().AddSingleton(sender).BuildServiceProvider();
        var notifier = new ExternalAuthenticationSecurityNotifier(services);
        var broker = CreateBroker(new RecordingAdapter(), notifier: notifier);

        var result = await broker.InitiateExternalAsync(Request("//attacker.example"), "tenant-a");

        Assert.Equal("invalid_request", result.Error?.Error);
        await sender.Received(1).SendAsync(
            Arg.Is<ExternalAuthenticationOutcomeRecorded>(notification =>
                notification.Flow == "external" &&
                notification.Stage == "initiate" &&
                notification.Category == "invalid_request" &&
                notification.Context.Outcome == SecurityEventOutcome.Rejected),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdapterInitiationFailureReturnsASafeObservedOutcome()
    {
        var sender = Substitute.For<INotificationSender>();
        await using var services = new ServiceCollection().AddSingleton(sender).BuildServiceProvider();
        var broker = CreateBroker(
            new RecordingAdapter { ThrowOnInitiation = true },
            notifier: new ExternalAuthenticationSecurityNotifier(services));

        var result = await broker.InitiateExternalAsync(Request("/workflows"), "tenant-a");

        Assert.Equal("temporarily_unavailable", result.Error?.Error);
        await sender.Received(1).SendAsync(
            Arg.Is<ExternalAuthenticationOutcomeRecorded>(notification =>
                notification.Category == "temporarily_unavailable" &&
                notification.Context.Outcome == SecurityEventOutcome.Failed),
            Arg.Any<CancellationToken>());
    }

    internal static ExternalAuthenticationBroker CreateBroker(
        RecordingAdapter adapter,
        IAuthorizationGrantStore? grants = null,
        IExternalAuthenticationHandleHasher? hasher = null,
        IEnumerable<ISecretBindingResolver>? resolvers = null,
        IReadOnlyCollection<AuthenticationClient>? clients = null,
        Action<IdentityProviderConnection>? configureConnection = null,
        IExternalIdentityResolver? identityResolver = null,
        IPermissionGrantResolver? permissionGrantResolver = null,
        IExternalAuthenticationSessionStore? sessionStore = null,
        ExternalAuthenticationSecurityNotifier? notifier = null)
    {
        var connection = new IdentityProviderConnection
        {
            Id = "connection-a", TenantId = "tenant-a", Key = "contoso", AdapterType = "fake", AdapterSettings = JsonSerializer.SerializeToElement(new { }),
            DisplayName = "Contoso", IsEnabled = true, MaterialRevision = "revision-a"
        };
        configureConnection?.Invoke(connection);
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, new(ConnectionScopeKind.Tenant, "tenant-a"), ConnectionValidity.Valid, false, "test");
        var registry = Substitute.For<IIdentityProviderConnectionRegistry>();
        registry.FindByKeyAsync("tenant-a", "contoso", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effective));
        registry.FindByIdAsync("tenant-a", "connection-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effective));
        registry.GetAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(new EffectiveConnectionRegistry([effective], [], "v1")));
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions
        {
            Clients = clients?.ToList() ?? [new AuthenticationClient("studio", "Studio", AuthenticationClientType.Public,
                new HashSet<Uri> { new("https://studio.example/authentication/external/callback") }, new HashSet<Uri>(), new HashSet<string> { "https://studio.example" }, new HashSet<string> { "/workflows" }, null, true)]
        });
        var clock = new TestClock();
        return new ExternalAuthenticationBroker(registry, [adapter], resolvers ?? [], hasher ?? new HmacExternalAuthenticationHandleHasher(), new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider(), identityResolver ?? Substitute.For<IExternalIdentityResolver>(), permissionGrantResolver ?? Substitute.For<IPermissionGrantResolver>(), new InMemoryExternalAuthenticationStateStore(clock), grants ?? new InMemoryAuthorizationGrantStore(clock), sessionStore ?? new InMemoryExternalAuthenticationSessionStore(clock), Substitute.For<IExternalAuthenticationTokenIssuer>(), Substitute.For<IUserCredentialsValidator>(), Substitute.For<IUserProvider>(), Substitute.For<IRoleProvider>(), Substitute.For<IElsaTokenService>(), clock, options, notifier);
    }

    private static BrokerAuthorizationRequest Request(string returnPath) => new("studio", new Uri("https://studio.example/authentication/external/callback"), "code", "challenge", "S256", returnPath, "contoso");
    private static string? Query(Uri uri, string key) => System.Web.HttpUtility.ParseQueryString(uri.Query)[key];

    private sealed class TestClock : ISystemClock { public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-01-01T00:00:00Z"); }

    internal sealed class RecordingAdapter : IExternalAuthenticationAdapter
    {
        public string Type => "fake";
        public string? CorrelationState { get; private set; }
        public BrokerTransaction? Transaction { get; private set; }
        public EffectiveIdentityProviderConnection? Connection { get; private set; }
        public bool ThrowOnInitiation { get; init; }
        public bool ThrowOnCallback { get; init; }
        public ExternalAuthenticationResult? AuthenticationResult { get; init; }
        public ExternalAuthenticationAdapterDescriptor Describe() => throw new NotSupportedException();
        public ValueTask<ConnectionValidationResult> ValidateAsync(ConnectionValidationContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalAuthorizationRequest> CreateAuthorizationRequestAsync(ExternalAuthorizationContext context, CancellationToken cancellationToken = default)
        {
            if (ThrowOnInitiation)
                throw new InvalidOperationException();
            CorrelationState = context.CorrelationState;
            Transaction = context.Transaction;
            Connection = context.Connection;
            return ValueTask.FromResult(new ExternalAuthorizationRequest(new Uri($"https://issuer.example/authorize?state={Uri.EscapeDataString(context.CorrelationState)}"), [1, 2, 3]));
        }
        public ValueTask<ExternalAuthenticationResult> AuthenticateCallbackAsync(ExternalCallbackContext context, CancellationToken cancellationToken = default)
        {
            if (ThrowOnCallback)
                throw new InvalidOperationException();
            if (AuthenticationResult is not null)
                return ValueTask.FromResult(AuthenticationResult);
            throw new NotSupportedException();
        }
        public ValueTask<ConnectionTestResult> TestAsync(ConnectionTestContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<ExternalLogoutRequest?> CreateLogoutRequestAsync(ExternalLogoutContext context, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FixedHasher : IExternalAuthenticationHandleHasher
    {
        public string Hash(string value) => $"hash:{value}";
    }

    private sealed class MutableSecretResolver : ISecretBindingResolver
    {
        public string Type => "test";
        public string Generation { get; set; } = "generation-1";
        public ValueTask<SecretBindingState> GetStateAsync(SecretBinding binding, CancellationToken cancellationToken = default) => ValueTask.FromResult(new SecretBindingState(true, true));
        public ValueTask<ResolvedSecretBinding> ResolveAsync(SecretBinding binding, CancellationToken cancellationToken = default) => ValueTask.FromResult(new ResolvedSecretBinding(new SensitiveString("secret"), Generation));
    }
}
