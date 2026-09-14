using Elsa.Testing.Shared.Multitenancy;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Policies;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Broker;

public class BrokerSecurityTests
{
    [Fact]
    public async Task LocalAuthorizationCodeExchangeAndRefreshResolveCurrentPermissionsInTheTokenTenant()
    {
        const string verifier = "local-login-code-verifier";
        var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var tenantAccessor = new DefaultTenantAccessor();
        var user = new User { Id = "user-a", Name = "admin", TenantId = "tenant-a", Roles = ["admin"] };
        var role = new Role { Id = "admin", Name = "Administrator", TenantId = "tenant-a", Permissions = ["*"] };
        var credentials = Substitute.For<IUserCredentialsValidator>();
        credentials.ValidateAsync("admin", "password", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<User?>(user));
        var users = Substitute.For<IUserProvider>();
        users.FindAsync(Arg.Any<UserFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(tenantAccessor.TenantId == "tenant-a" ? user : null));
        var roles = Substitute.For<IRoleProvider>();
        roles.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult<IEnumerable<Role>>(tenantAccessor.TenantId == "tenant-a" ? [role] : []));
        var tokenOptions = Microsoft.Extensions.Options.Options.Create(new IdentityTokenOptions
        {
            SigningKey = "local-external-authentication-test-signing-key",
            Issuer = "https://elsa.test",
            Audience = "elsa-api"
        });
        var tokens = new DefaultElsaTokenService(new CurrentTestClock(), tokenOptions);
        var refreshTokens = new DefaultIdentityRefreshTokenService(users, new DefaultAccessTokenIssuer(roles, tokens), tenantAccessor, tokenOptions);
        var externalTokenIssuer = Substitute.For<IExternalAuthenticationTokenIssuer>();
        externalTokenIssuer.RefreshAsync("studio", Arg.Any<SensitiveString>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException<ExternalTokenResponse>(new InvalidOperationException("A local refresh token must not use the external-session issuer.")));
        var broker = CreateBroker(
            new RecordingAdapter(),
            tokenIssuer: externalTokenIssuer,
            credentialsValidator: credentials,
            userProvider: users,
            roleProvider: roles,
            tokenService: tokens,
            identityRefreshTokenService: refreshTokens,
            tenantAccessor: tenantAccessor);
        BrokerCallbackResult authorization;
        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-a", Name = "Tenant A" }))
        {
            authorization = await broker.InitiateLocalAsync(
                new LocalBrokerAuthorizationRequest(
                    "studio",
                    new Uri("https://studio.example/authentication/external/callback"),
                    "code",
                    challenge,
                    "S256",
                    "/workflows",
                    "admin",
                    "password"),
                "tenant-a");
        }
        var code = Query(authorization.RedirectUri!, "code");

        BrokerTokenResult exchange;
        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "Tenant B" }))
        {
            exchange = await broker.ExchangeAsync(new BrokerTokenRequest(
                "authorization_code",
                "studio",
                new Uri("https://studio.example/authentication/external/callback"),
                code,
                verifier,
                null,
                "https://studio.example"));

            Assert.Equal("tenant-b", tenantAccessor.TenantId);
        }

        Assert.Null(exchange.Error);
        var accessToken = new JsonWebTokenHandler().ReadJsonWebToken(exchange.Token!.AccessToken);
        Assert.Contains(accessToken.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id);
        Assert.Contains(accessToken.Claims, claim => claim.Type == "permissions" && claim.Value == "*");

        role.Permissions = ["workflows:manage"];
        var refresh = await broker.ExchangeAsync(new BrokerTokenRequest(
            "refresh_token",
            "studio",
            null,
            null,
            null,
            exchange.Token.RefreshToken,
            "https://studio.example"));

        Assert.Null(refresh.Error);
        Assert.NotNull(refresh.Token);
        Assert.True(exchange.Token.RefreshExpiresIn > 0);
        Assert.True(refresh.Token.RefreshExpiresIn > 0);
        var refreshedAccessToken = new JsonWebTokenHandler().ReadJsonWebToken(refresh.Token.AccessToken);
        Assert.Contains(refreshedAccessToken.Claims, claim => claim.Type == "permissions" && claim.Value == "workflows:manage");
        Assert.DoesNotContain(refreshedAccessToken.Claims, claim => claim.Type == "permissions" && claim.Value == "*");
        await externalTokenIssuer.DidNotReceive().RefreshAsync(Arg.Any<string>(), Arg.Any<SensitiveString>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LocalInitiationTreatsNullUserTenantAsTheDefaultTenant()
    {
        var credentials = Substitute.For<IUserCredentialsValidator>();
        credentials.ValidateAsync("admin", "password", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<User?>(new User { Id = "admin", Name = "admin" }));
        var broker = CreateBroker(new RecordingAdapter(), credentialsValidator: credentials);
        var request = new LocalBrokerAuthorizationRequest(
            "studio", new Uri("https://studio.example/authentication/external/callback"), "code", "challenge", "S256", "/workflows", "admin", "password", "state");

        var result = await broker.InitiateLocalAsync(request, Tenant.DefaultTenantId);

        Assert.Null(result.Error);
        Assert.StartsWith("https://studio.example/authentication/external/callback?code=", result.RedirectUri?.AbsoluteUri);
        Assert.Contains("state=state", result.RedirectUri?.Query);
    }

    [Fact]
    public async Task OpaqueRefreshTokensContinueToUseTheExternalSessionIssuer()
    {
        var expected = new ExternalTokenResponse("access", "Bearer", 300, "session.rotated", 600, 600);
        var externalTokenIssuer = Substitute.For<IExternalAuthenticationTokenIssuer>();
        externalTokenIssuer.RefreshAsync("studio", Arg.Any<SensitiveString>(), Arg.Any<CancellationToken>()).Returns(expected);
        var identityRefreshTokenService = Substitute.For<IIdentityRefreshTokenService>();
        var broker = CreateBroker(
            new RecordingAdapter(),
            tokenIssuer: externalTokenIssuer,
            identityRefreshTokenService: identityRefreshTokenService);

        var result = await broker.ExchangeAsync(new BrokerTokenRequest(
            "refresh_token",
            "studio",
            null,
            null,
            null,
            "session.random",
            "https://studio.example"));

        Assert.Null(result.Error);
        Assert.Same(expected, result.Token);
        await identityRefreshTokenService.DidNotReceive().RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

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

        var result = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] });

        Assert.Equal("authentication_failed", result.Error?.Error);
        Assert.StartsWith("https://studio.example/authentication/external/callback?", result.RedirectUri?.AbsoluteUri);
        Assert.DoesNotContain("issuer.example", result.RedirectUri?.AbsoluteUri);
    }

    [Fact]
    public async Task SuccessfulExternalSignInRecordsTheTimestampForAnExistingIdentityLink()
    {
        var scenario = CreateIdentityLinkTrackingScenario();
        var existing = await scenario.Provisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", scenario.Identity, new UserCreationProposal("external")));
        var signedInAt = new DateTimeOffset(2026, 1, 1, 0, 1, 0, TimeSpan.Zero);
        scenario.Clock.UtcNow = signedInAt;

        var result = await CompleteExternalSignInAsync(scenario);
        var link = await scenario.Provisioner.FindLinkAsync("tenant-a", "contoso", scenario.Identity);

        Assert.Null(result.Error);
        Assert.Equal(existing.Link.Id, link?.Id);
        Assert.Equal(signedInAt, link?.LastSignedInAt);
    }

    [Fact]
    public async Task SuccessfulExternalSignInRecordsTheInitialTimestampForANewIdentityLink()
    {
        var scenario = CreateIdentityLinkTrackingScenario();
        var signedInAt = new DateTimeOffset(2026, 1, 1, 0, 1, 0, TimeSpan.Zero);
        scenario.Clock.UtcNow = signedInAt;

        var result = await CompleteExternalSignInAsync(scenario);
        var link = await scenario.Provisioner.FindLinkAsync("tenant-a", "contoso", scenario.Identity);

        Assert.Null(result.Error);
        Assert.NotNull(link);
        Assert.Equal(signedInAt, link.LastSignedInAt);
    }

    [Fact]
    public async Task RepeatSuccessfulExternalSignInReplacesTheIdentityLinkTimestamp()
    {
        var scenario = CreateIdentityLinkTrackingScenario();
        await scenario.Provisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", scenario.Identity, new UserCreationProposal("external")));
        var firstSignInAt = new DateTimeOffset(2026, 1, 1, 0, 1, 0, TimeSpan.Zero);
        scenario.Clock.UtcNow = firstSignInAt;
        await CompleteExternalSignInAsync(scenario);
        var secondSignInAt = firstSignInAt.AddMinutes(1);
        scenario.Clock.UtcNow = secondSignInAt;

        var result = await CompleteExternalSignInAsync(scenario);
        var link = await scenario.Provisioner.FindLinkAsync("tenant-a", "contoso", scenario.Identity);

        Assert.Null(result.Error);
        Assert.Equal(secondSignInAt, link?.LastSignedInAt);
    }

    [Fact]
    public async Task UnsuccessfulExternalSignInDoesNotRecordTheIdentityLinkTimestamp()
    {
        var scenario = CreateIdentityLinkTrackingScenario(throwOnCallback: true);
        await scenario.Provisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest("tenant-a", "contoso", scenario.Identity, new UserCreationProposal("external")));
        scenario.Clock.UtcNow = new DateTimeOffset(2026, 1, 1, 0, 1, 0, TimeSpan.Zero);

        var result = await CompleteExternalSignInAsync(scenario);
        var link = await scenario.Provisioner.FindLinkAsync("tenant-a", "contoso", scenario.Identity);

        Assert.Equal("authentication_failed", result.Error?.Error);
        Assert.Null(link?.LastSignedInAt);
    }

    [Fact]
    public async Task ProviderCallbackStateCannotBeReplayed()
    {
        var adapter = new RecordingAdapter { ThrowOnCallback = true };
        var broker = CreateBroker(adapter);
        await broker.InitiateExternalAsync(Request("/workflows"), "tenant-a");
        var parameters = new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] };

        _ = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, parameters);
        var replay = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, parameters);

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

        var result = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] });

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
        registry.FindByKeyAsync("tenant-a", "contoso", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effective));
        var users = Substitute.For<IUserProvider>();
        users.FindAsync(Arg.Any<UserFilter>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" }));
        var roles = Substitute.For<IRoleProvider>();
        roles.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<IEnumerable<Role>>([]));
        var tokens = Substitute.For<IElsaTokenService>();
        tokens.IssueAccessTokenAsync(Arg.Any<TokenIssuanceContext>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(new IssuedAccessToken("access", clock.UtcNow.AddHours(1))));
        var issuer = new DefaultExternalAuthenticationTokenIssuer(store, registry, [], users, roles, tokens, new DefaultTenantAccessor(), clock, Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions()));
        var session = new ExternalAuthenticationSession { Id = "session-a", AuthenticationClientId = "studio", TenantId = "tenant-a", UserId = "user-a", ConnectionKey = "contoso", ConnectionMaterialRevision = "revision-a", SecretGenerationFingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData([])), Issuer = "issuer", SubjectHash = "subject", StartedAt = clock.UtcNow, LastRefreshedAt = clock.UtcNow, ExpiresAt = clock.UtcNow.AddHours(1), RefreshExpiresAt = clock.UtcNow.AddHours(1) };

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

        var result = await broker.CompleteCallbackAsync("contoso", adapter.CorrelationState!, new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] });

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
        IExternalAuthenticationTokenIssuer? tokenIssuer = null,
        IUserCredentialsValidator? credentialsValidator = null,
        IUserProvider? userProvider = null,
        IRoleProvider? roleProvider = null,
        IElsaTokenService? tokenService = null,
        IIdentityRefreshTokenService? identityRefreshTokenService = null,
        ITenantAccessor? tenantAccessor = null,
        ExternalAuthenticationSecurityNotifier? notifier = null,
        ConnectionValidity connectionValidity = ConnectionValidity.Valid,
        ConnectionValidity? assessedValidity = null,
        bool includeLoginMethod = false,
        ISystemClock? clock = null)
    {
        var connection = new IdentityProviderConnection
        {
            Id = "connection-a", TenantId = "tenant-a", Key = "contoso", AdapterType = "fake", AdapterSettings = JsonSerializer.SerializeToElement(new { }),
            DisplayName = "Contoso", IsEnabled = true, MaterialRevision = "revision-a"
        };
        configureConnection?.Invoke(connection);
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, new(ConnectionScopeKind.Tenant, "tenant-a"), connectionValidity, false, "test");
        var registry = Substitute.For<IIdentityProviderConnectionRegistry>();
        registry.FindByKeyAsync("tenant-a", "contoso", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effective));
        registry.FindByIdAsync("tenant-a", "connection-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effective));
        IReadOnlyCollection<LoginMethod> loginMethods = includeLoginMethod
            ? [new LoginMethod(connection.Id, connection.Key, LoginMethodKind.External, connection.DisplayName, null, 0, false, new Uri($"/external-authentication/authorize/{connection.Key}", UriKind.Relative))]
            : [];
        registry.GetAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(new EffectiveConnectionRegistry([effective], loginMethods, "v1")));
        var validityAssessor = Substitute.For<IIdentityProviderConnectionValidityAssessor>();
        validityAssessor.AssessAsync(Arg.Any<EffectiveIdentityProviderConnection>(), Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult(call.Arg<EffectiveIdentityProviderConnection>() with
            {
                Validity = assessedValidity ?? call.Arg<EffectiveIdentityProviderConnection>().Validity
            }));
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions
        {
            Clients = clients?.ToList() ?? [new AuthenticationClient("studio", "Studio", AuthenticationClientType.Public,
                new HashSet<Uri> { new("https://studio.example/authentication/external/callback") }, new HashSet<Uri>(), new HashSet<string> { "https://studio.example" }, new HashSet<string> { "/workflows" }, null, true)]
        });
        var brokerClock = clock ?? new TestClock();
        return new ExternalAuthenticationBroker(registry, validityAssessor, [adapter], resolvers ?? [], hasher ?? new HmacExternalAuthenticationHandleHasher(), new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider(), identityResolver ?? Substitute.For<IExternalIdentityResolver>(), permissionGrantResolver ?? Substitute.For<IPermissionGrantResolver>(), new InMemoryExternalAuthenticationStateStore(brokerClock), grants ?? new InMemoryAuthorizationGrantStore(brokerClock), sessionStore ?? new InMemoryExternalAuthenticationSessionStore(brokerClock), tokenIssuer ?? Substitute.For<IExternalAuthenticationTokenIssuer>(), credentialsValidator ?? Substitute.For<IUserCredentialsValidator>(), userProvider ?? Substitute.For<IUserProvider>(), roleProvider ?? Substitute.For<IRoleProvider>(), tokenService ?? Substitute.For<IElsaTokenService>(), identityRefreshTokenService ?? Substitute.For<IIdentityRefreshTokenService>(), tenantAccessor ?? new DefaultTenantAccessor(), brokerClock, options, notifier);
    }

    private static BrokerAuthorizationRequest Request(string returnPath) => new("studio", new Uri("https://studio.example/authentication/external/callback"), "code", "challenge", "S256", returnPath, "contoso");
    private static string? Query(Uri uri, string key) => System.Web.HttpUtility.ParseQueryString(uri.Query)[key];

    private static IdentityLinkTrackingScenario CreateIdentityLinkTrackingScenario(bool throwOnCallback = false)
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var identity = new ExternalIdentity("https://issuer.example", "subject-a", new Dictionary<string, IReadOnlyCollection<string>>());
        var users = new MemoryUserStore(new MemoryStore<User>(), new TestTenantAccessor("tenant-a"));
        var provisioner = new InMemoryExternalIdentityProvisioner(
            users,
            new StoreBasedUserProvider(users),
            Substitute.For<IRoleProvider>(),
            new GuidIdentityGenerator(),
            clock,
            new FixedHasher(),
            new InMemoryExternalIdentityProvisionerState());
        var identityResolver = new DefaultExternalIdentityResolver(
            provisioner,
            [new CreateUserUnlinkedIdentityPolicy()],
            Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions
            {
                UnlinkedIdentityPolicy = new UnlinkedIdentityPolicyOptions { DefaultType = "create-user" }
            }));
        var permissionGrantResolver = Substitute.For<IPermissionGrantResolver>();
        permissionGrantResolver.ResolveAsync(Arg.Any<PermissionGrantResolutionContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PermissionGrantResult([], [])));
        var adapter = new RecordingAdapter
        {
            ThrowOnCallback = throwOnCallback,
            AuthenticationResult = new ExternalAuthenticationResult(identity, identity.Claims, [])
        };
        var broker = CreateBroker(adapter, identityResolver: identityResolver, permissionGrantResolver: permissionGrantResolver, clock: clock);

        return new IdentityLinkTrackingScenario(broker, provisioner, adapter, identity, clock);
    }

    private static async Task<BrokerCallbackResult> CompleteExternalSignInAsync(IdentityLinkTrackingScenario scenario)
    {
        await scenario.Broker.InitiateExternalAsync(Request("/workflows"), "tenant-a");
        return await scenario.Broker.CompleteCallbackAsync("contoso", scenario.Adapter.CorrelationState!, new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [scenario.Adapter.CorrelationState!] });
    }

    private sealed class TestClock : ISystemClock { public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-01-01T00:00:00Z"); }
    private sealed class CurrentTestClock : ISystemClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
    private sealed class MutableClock(DateTimeOffset utcNow) : ISystemClock { public DateTimeOffset UtcNow { get; set; } = utcNow; }

    private sealed record IdentityLinkTrackingScenario(
        ExternalAuthenticationBroker Broker,
        InMemoryExternalIdentityProvisioner Provisioner,
        RecordingAdapter Adapter,
        ExternalIdentity Identity,
        MutableClock Clock);

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
