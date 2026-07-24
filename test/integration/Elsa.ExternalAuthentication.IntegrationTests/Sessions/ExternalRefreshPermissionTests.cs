using System.Security.Cryptography;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.IntegrationTests.Broker;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Sessions;

public class ExternalRefreshPermissionTests
{
    [Fact]
    public async Task BrokerSnapshotsResolvedExternalGrantsWithProvenance()
    {
        var identity = new ExternalIdentity("https://issuer.example", "subject-a", new Dictionary<string, IReadOnlyCollection<string>>());
        var adapter = new BrokerSecurityTests.RecordingAdapter
        {
            AuthenticationResult = new ExternalAuthenticationResult(identity, new Dictionary<string, IReadOnlyCollection<string>> { ["groups"] = ["operators"] }, [])
        };
        var identityResolver = Substitute.For<IExternalIdentityResolver>();
        identityResolver.ResolveAsync(Arg.Any<ExternalIdentityResolutionContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new ExternalIdentityResolution("user-a", false)));
        var permissionResolver = Substitute.For<IPermissionGrantResolver>();
        var expectedGrant = new PermissionGrant("workflows:manage", "group-mapping", "groups:operators");
        permissionResolver.ResolveAsync(Arg.Any<PermissionGrantResolutionContext>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(new PermissionGrantResult([expectedGrant], [])));
        var sessions = Substitute.For<IExternalAuthenticationSessionStore>();
        ExternalAuthenticationSession? savedSession = null;
        sessions.SaveAsync(Arg.Do<ExternalAuthenticationSession>(session => savedSession = session), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        var broker = BrokerSecurityTests.CreateBroker(
            adapter,
            identityResolver: identityResolver,
            permissionGrantResolver: permissionResolver,
            sessionStore: sessions);
        var request = new BrokerAuthorizationRequest(
            "studio",
            new Uri("https://studio.example/authentication/external/callback"),
            "code",
            "challenge",
            "S256",
            "/workflows",
            "contoso");
        await broker.InitiateExternalAsync(request, "tenant-a");

        var result = await broker.CompleteCallbackAsync(
            "connection-a",
            adapter.CorrelationState!,
            new Dictionary<string, IReadOnlyCollection<string>> { ["state"] = [adapter.CorrelationState!] });

        Assert.Null(result.Error);
        Assert.NotNull(savedSession);
        Assert.Equal([expectedGrant], savedSession.ExternalGrants);
        await permissionResolver.Received(1).ResolveAsync(
            Arg.Is<PermissionGrantResolutionContext>(context =>
                context.TargetTenantId == "tenant-a" &&
                context.UserId == "user-a" &&
                context.Identity == identity &&
                context.ProjectedClaims.ContainsKey("groups")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshRetainsBoundedExternalSnapshotAndReevaluatesCurrentElsaRoles()
    {
        var clock = new TestClock();
        var sessionStore = new InMemoryExternalAuthenticationSessionStore(clock);
        var connection = new IdentityProviderConnection
        {
            Id = "connection-a",
            TenantId = "tenant-a",
            Key = "contoso",
            AdapterType = "openid-connect",
            DisplayName = "Contoso",
            IsEnabled = true,
            MaterialRevision = "revision-a"
        };
        var effective = new EffectiveIdentityProviderConnection(
            connection,
            ConnectionSourceOwnership.Configuration,
            new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"),
            ConnectionValidity.Valid,
            false,
            "configuration");
        var registry = Substitute.For<IIdentityProviderConnectionRegistry>();
        registry.FindByIdAsync("tenant-a", "connection-a", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effective));
        var user = new User { Id = "user-a", Name = "alice", TenantId = "tenant-a", Roles = ["role-a"] };
        var role = new Role { Id = "role-a", Name = "Operators", TenantId = "tenant-a", Permissions = ["workflows:read"] };
        var users = Substitute.For<IUserProvider>();
        users.FindAsync(Arg.Any<UserFilter>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<User?>(user));
        var roles = Substitute.For<IRoleProvider>();
        roles.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult<IEnumerable<Role>>([role]));
        var issuanceContexts = new List<TokenIssuanceContext>();
        var tokenService = Substitute.For<IElsaTokenService>();
        tokenService.IssueAccessTokenAsync(
                Arg.Do<TokenIssuanceContext>(context => issuanceContexts.Add(context)),
                Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult(new IssuedAccessToken($"access-{issuanceContexts.Count}", clock.UtcNow.AddHours(1))));
        var issuer = new DefaultExternalAuthenticationTokenIssuer(sessionStore, registry, [], users, roles, tokenService, clock);
        var externalGrant = new PermissionGrant("reports:view", "claim-mapping", "department:engineering");
        var session = new ExternalAuthenticationSession
        {
            Id = "session-a",
            AuthenticationClientId = "studio",
            TenantId = "tenant-a",
            UserId = "user-a",
            ConnectionId = "connection-a",
            ConnectionMaterialRevision = "revision-a",
            SecretGenerationFingerprint = Convert.ToHexString(SHA256.HashData([])),
            Issuer = "https://issuer.example",
            SubjectHash = "subject-hash",
            ExternalGrants = [externalGrant],
            StartedAt = clock.UtcNow,
            LastRefreshedAt = clock.UtcNow,
            ExpiresAt = clock.UtcNow.AddHours(8),
            RefreshExpiresAt = clock.UtcNow.AddHours(8)
        };

        var initial = await issuer.IssueAsync(session);
        role.Permissions = ["workflows:manage"];
        using var refreshToken = new SensitiveString(initial.RefreshToken);
        await issuer.RefreshAsync("studio", refreshToken);

        Assert.Equal(2, issuanceContexts.Count);
        Assert.Equal(["workflows:read", "reports:view"], issuanceContexts[0].Permissions);
        Assert.Equal(["workflows:manage", "reports:view"], issuanceContexts[1].Permissions);
        Assert.DoesNotContain("workflows:read", issuanceContexts[1].Permissions);
        Assert.Equal("session-a", issuanceContexts[1].ExternalAuthenticationSessionId);
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-07-24T12:00:00Z");
    }
}
