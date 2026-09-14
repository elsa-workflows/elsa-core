using System.Security.Cryptography;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.IntegrationTests.Broker;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
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
            "contoso",
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
        registry.FindByKeyAsync("tenant-a", "contoso", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effective));
        var user = new User { Id = "user-a", Name = "alice", TenantId = "tenant-a", Roles = ["role-a"] };
        var role = new Role { Id = "role-a", Name = "Operators", TenantId = "tenant-a", Permissions = ["*"] };
        var tenantAccessor = new DefaultTenantAccessor();
        var users = Substitute.For<IUserProvider>();
        users.FindAsync(Arg.Any<UserFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(tenantAccessor.TenantId == "tenant-a" ? user : null));
        var roles = Substitute.For<IRoleProvider>();
        roles.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult<IEnumerable<Role>>(tenantAccessor.TenantId == "tenant-a" ? [role] : []));
        var issuanceContexts = new List<TokenIssuanceContext>();
        var tokenService = Substitute.For<IElsaTokenService>();
        tokenService.IssueAccessTokenAsync(
                Arg.Do<TokenIssuanceContext>(context => issuanceContexts.Add(context)),
                Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult(new IssuedAccessToken($"access-{issuanceContexts.Count}", clock.UtcNow.AddHours(1))));
        var issuer = new DefaultExternalAuthenticationTokenIssuer(sessionStore, registry, [], users, roles, tokenService, tenantAccessor, clock, Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions()));
        var externalGrant = new PermissionGrant("reports:view", "claim-mapping", "department:engineering");
        var session = new ExternalAuthenticationSession
        {
            Id = "session-a",
            AuthenticationClientId = "studio",
            TenantId = "tenant-a",
            UserId = "user-a",
            ConnectionKey = "contoso",
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

        using (tenantAccessor.PushContext(new Tenant { Id = "tenant-b", Name = "Tenant B" }))
        {
            var initial = await issuer.IssueAsync(session);
            role.Permissions = ["workflows:manage"];
            using var refreshToken = new SensitiveString(initial.RefreshToken);
            await issuer.RefreshAsync("studio", refreshToken);

            Assert.Equal("tenant-b", tenantAccessor.TenantId);
        }

        Assert.Equal(2, issuanceContexts.Count);
        Assert.Equal(["*", "reports:view"], issuanceContexts[0].Permissions);
        Assert.Equal(["workflows:manage", "reports:view"], issuanceContexts[1].Permissions);
        Assert.DoesNotContain("*", issuanceContexts[1].Permissions);
        Assert.Equal("session-a", issuanceContexts[1].ExternalAuthenticationSessionId);
    }

    [Fact]
    public async Task DeploymentDenyBoundaryAppliesToRoleDerivedPermissionsAtIssuance()
    {
        // A role permission excluded by the boundary during grant resolution used to reappear here, because
        // issuance concatenated the same roles' permissions raw. That made the deny list unenforceable for
        // anything a role carried, whether or not the connection selected the elsa-roles grant source.
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.DeniedPermissions = ["workflows/*:delete"];

        var contexts = await IssueWithAsync(options, rolePermissions: ["workflows/definitions:delete", "workflows/definitions:view"]);

        // The denied role permission is gone; the role's other permission and the undenied external grant stay.
        Assert.DoesNotContain("workflows/definitions:delete", contexts.Single().Permissions);
        Assert.Equal(["workflows/definitions:view", "reports:view"], contexts.Single().Permissions);
    }

    [Fact]
    public async Task RolePermissionsAreUnaffectedWhenNoBoundaryIsConfigured()
    {
        // The default is both lists empty, and that must stay a no-op: an external login should not quietly
        // hand back less than the user's roles grant just because issuance now consults the boundary.
        var contexts = await IssueWithAsync(new ExternalAuthenticationOptions(), rolePermissions: ["workflows/definitions:delete", "*"]);

        Assert.Equal(["workflows/definitions:delete", "*", "reports:view"], contexts.Single().Permissions);
    }

    private static async Task<IReadOnlyList<TokenIssuanceContext>> IssueWithAsync(ExternalAuthenticationOptions options, string[] rolePermissions)
    {
        var clock = new TestClock();
        var sessionStore = new InMemoryExternalAuthenticationSessionStore(clock);
        var connection = new IdentityProviderConnection
        {
            Id = "connection-a", TenantId = "tenant-a", Key = "contoso", AdapterType = "oidc",
            AdapterSettingsVersion = 1, DisplayName = "Contoso", MaterialRevision = "revision-a"
        };
        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), ConnectionValidity.Valid, false, "configuration");
        var registry = Substitute.For<IIdentityProviderConnectionRegistry>();
        registry.FindByKeyAsync("tenant-a", "contoso", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<EffectiveIdentityProviderConnection?>(effective));
        var user = new User { Id = "user-a", Name = "alice", TenantId = "tenant-a", Roles = ["role-a"] };
        var role = new Role { Id = "role-a", Name = "Operators", TenantId = "tenant-a", Permissions = rolePermissions };
        var tenantAccessor = new DefaultTenantAccessor();
        var users = Substitute.For<IUserProvider>();
        users.FindAsync(Arg.Any<UserFilter>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<User?>(user));
        var roles = Substitute.For<IRoleProvider>();
        roles.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>()).Returns(_ => ValueTask.FromResult<IEnumerable<Role>>([role]));
        var contexts = new List<TokenIssuanceContext>();
        var tokenService = Substitute.For<IElsaTokenService>();
        tokenService.IssueAccessTokenAsync(Arg.Do<TokenIssuanceContext>(contexts.Add), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromResult(new IssuedAccessToken($"access-{contexts.Count}", clock.UtcNow.AddHours(1))));
        var issuer = new DefaultExternalAuthenticationTokenIssuer(sessionStore, registry, [], users, roles, tokenService, tenantAccessor, clock, Microsoft.Extensions.Options.Options.Create(options));

        await issuer.IssueAsync(new ExternalAuthenticationSession
        {
            Id = "session-b", AuthenticationClientId = "studio", TenantId = "tenant-a", UserId = "user-a",
            ConnectionKey = "contoso", ConnectionMaterialRevision = "revision-a",
            SecretGenerationFingerprint = Convert.ToHexString(SHA256.HashData([])),
            Issuer = "https://issuer.example", SubjectHash = "subject-hash",
            ExternalGrants = [new PermissionGrant("reports:view", "claim-mapping", "department:engineering")],
            StartedAt = clock.UtcNow, LastRefreshedAt = clock.UtcNow,
            ExpiresAt = clock.UtcNow.AddHours(8), RefreshExpiresAt = clock.UtcNow.AddHours(8)
        });

        return contexts;
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-07-24T12:00:00Z");
    }
}
