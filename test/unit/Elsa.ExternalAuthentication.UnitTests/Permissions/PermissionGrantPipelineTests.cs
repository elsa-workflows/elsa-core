using System.Security.Claims;
using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.UnitTests.Permissions;

public class PermissionGrantPipelineTests
{
    [Fact]
    public async Task ComposesRoleAndMappedClaimGrantsInOrderWithDeterministicDeduplication()
    {
        var userProvider = new StaticUserProvider(new User { Id = "user-a", TenantId = "tenant-a", Roles = ["role-a"] });
        var roleProvider = new StaticRoleProvider(new Role { Id = "role-a", Name = "Operators", TenantId = "tenant-a", Permissions = ["workflows:read", "workflows:manage"] });
        var resolver = CreateResolver(userProvider, roleProvider, new ExternalAuthenticationOptions());
        var context = CreateContext(
            [
                new GrantSourceSelection("elsa-roles", 1, JsonSerializer.SerializeToElement(new { }), 0),
                new GrantSourceSelection("claim-mapping", 1, JsonSerializer.SerializeToElement(new { claimType = "department", mappings = new[] { new { value = "engineering", permissions = new[] { "workflows:read", "reports:view" } } } }), 1)
            ],
            new Dictionary<string, IReadOnlyCollection<string>> { ["department"] = ["engineering"] });

        var result = await resolver.ResolveAsync(context);

        Assert.Equal(["workflows:manage", "workflows:read", "reports:view"], result.Grants.Select(x => x.Permission));
        Assert.Equal("elsa-roles", result.Grants.First(x => x.Permission == "workflows:read").SourceType);
        Assert.Equal("role-a", result.Grants.First(x => x.Permission == "workflows:manage").SourceReference);
        Assert.Equal("department:engineering", result.Grants.Last().SourceReference);
    }

    [Fact]
    public async Task LeavesUnmappedClaimsUnauthorizedAndWarnsForUnknownDescriptorsWithoutRejectingThem()
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.AllowedPermissions = ["reports:view", "reports:blocked"];
        options.PermissionGrants.DeniedPermissions = ["reports:blocked"];
        var resolver = CreateResolver(new StaticUserProvider(null), new StaticRoleProvider(), options);
        var context = CreateContext(
            [new GrantSourceSelection("claim-mapping", 1, JsonSerializer.SerializeToElement(new { claimType = "department", mappings = new[] { new { value = "engineering", permissions = new[] { "reports:view", "reports:blocked" } } } }), 0)],
            new Dictionary<string, IReadOnlyCollection<string>> { ["department"] = ["sales"] });

        var unmapped = await resolver.ResolveAsync(context);
        Assert.Empty(unmapped.Grants);

        var mappedContext = context with { ProjectedClaims = new Dictionary<string, IReadOnlyCollection<string>> { ["department"] = ["engineering"] } };
        var mapped = await resolver.ResolveAsync(mappedContext);

        Assert.Equal(["reports:view"], mapped.Grants.Select(x => x.Permission));
        Assert.Contains(mapped.Warnings, warning => warning.Code == "permission_denied_by_deployment");
        Assert.Contains(mapped.Warnings, warning => warning.Code == "unknown_permission_descriptor" && warning.Message.Contains("reports:view", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SupportsTheQuickstartMappingObjectShapeAndEmitsEachWarningOnce()
    {
        var resolver = CreateResolver(new StaticUserProvider(null), new StaticRoleProvider(), new ExternalAuthenticationOptions());
        var settings = JsonSerializer.SerializeToElement(new { claimType = "groups", mappings = new Dictionary<string, string[]> { ["elsa-workflow-admins"] = ["workflows:read", "reports:view"] } });
        var context = CreateContext(
            [
                new GrantSourceSelection("group-mapping", 1, settings, 0),
                new GrantSourceSelection("group-mapping", 1, settings, 1)
            ],
            new Dictionary<string, IReadOnlyCollection<string>> { ["groups"] = ["elsa-workflow-admins"] });

        var result = await resolver.ResolveAsync(context);

        Assert.Equal(["reports:view", "workflows:read"], result.Grants.Select(x => x.Permission));
        Assert.Single(result.Warnings, warning => warning.Code == "unknown_permission_descriptor" && warning.Message.Contains("reports:view", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RepeatedDeniedGrantsEmitOneDeploymentBoundaryWarning()
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.DeniedPermissions = ["reports:view"];
        var resolver = CreateResolver(new StaticUserProvider(null), new StaticRoleProvider(), options);
        var settings = JsonSerializer.SerializeToElement(new { claimType = "department", mappings = new Dictionary<string, string[]> { ["engineering"] = ["reports:view"] } });
        var context = CreateContext(
            [
                new GrantSourceSelection("claim-mapping", 1, settings, 0),
                new GrantSourceSelection("claim-mapping", 1, settings, 1)
            ],
            new Dictionary<string, IReadOnlyCollection<string>> { ["department"] = ["engineering"] });

        var result = await resolver.ResolveAsync(context);

        Assert.Empty(result.Grants);
        Assert.Single(result.Warnings, warning => warning.Code == "permission_denied_by_deployment");
    }

    [Fact]
    public async Task PassThroughClaimsGrantNothingWithoutAnExplicitNonEmptyBoundary()
    {
        var resolver = CreateResolver(new StaticUserProvider(null), new StaticRoleProvider(), new ExternalAuthenticationOptions());
        var empty = CreateContext(
            [new GrantSourceSelection("claim-pass-through", 1, JsonSerializer.SerializeToElement(new { claimType = "permissions", allowedPermissions = Array.Empty<string>() }), 0)],
            new Dictionary<string, IReadOnlyCollection<string>> { ["permissions"] = ["reports:view", "workflows:manage"] });

        var emptyResult = await resolver.ResolveAsync(empty);
        Assert.Empty(emptyResult.Grants);

        var bounded = empty with
        {
            Connection = new EffectiveIdentityProviderConnection(new IdentityProviderConnection
            {
                Id = "connection-a", TenantId = "tenant-a", Key = "contoso", AdapterType = "oidc", AdapterSettingsVersion = 1, DisplayName = "Contoso",
                PermissionGrantSources = [new GrantSourceSelection("claim-pass-through", 1, JsonSerializer.SerializeToElement(new { claimType = "permissions", allowedPermissions = new[] { "reports:view" } }), 0)]
            }, ConnectionSourceOwnership.Configuration, new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), ConnectionValidity.Valid, false, "test")
        };
        var boundedResult = await resolver.ResolveAsync(bounded);

        Assert.Equal(["reports:view"], boundedResult.Grants.Select(x => x.Permission));
    }

    [Fact]
    public async Task DelegationRequiresTheActorToPossessMappedPermissionsUnlessUnrestrictedAndStillHonorsDeploymentDeny()
    {
        var selection = new GrantSourceSelection("group-mapping", 1, JsonSerializer.SerializeToElement(new { claimType = "groups", mappings = new Dictionary<string, string[]> { ["operators"] = ["workflows:manage"] } }), 0);
        var options = new ExternalAuthenticationOptions();
        var authorizer = new DefaultPermissionDelegationAuthorizer(Microsoft.Extensions.Options.Options.Create(options));
        var ordinaryActor = CreateActor(ExternalAuthenticationPermissions.PermissionsDelegate, "workflows:read");

        var ordinary = await authorizer.AuthorizeAsync(ordinaryActor, [selection]);

        Assert.False(ordinary.IsAuthorized);
        Assert.Equal(["workflows:manage"], ordinary.UnauthorizedPermissions);

        options.PermissionGrants.DeniedPermissions = ["workflows:manage"];
        var unrestricted = await authorizer.AuthorizeAsync(CreateActor(ExternalAuthenticationPermissions.PermissionsDelegateUnrestricted), [selection]);

        Assert.False(unrestricted.IsAuthorized);
        Assert.Equal(["workflows:manage"], unrestricted.UnauthorizedPermissions);
    }

    [Fact]
    public async Task DelegationRequiresTheActorToPossessEachExplicitPassThroughPermission()
    {
        var selection = new GrantSourceSelection("claim-pass-through", 1, JsonSerializer.SerializeToElement(new { claimType = "permissions", allowedPermissions = new[] { "reports:view" } }), 0);
        var authorizer = new DefaultPermissionDelegationAuthorizer(Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions()));

        var denied = await authorizer.AuthorizeAsync(CreateActor(ExternalAuthenticationPermissions.PermissionsDelegate), [selection]);
        var allowed = await authorizer.AuthorizeAsync(CreateActor(ExternalAuthenticationPermissions.PermissionsDelegate, "reports:view"), [selection]);

        Assert.False(denied.IsAuthorized);
        Assert.Equal(["reports:view"], denied.UnauthorizedPermissions);
        Assert.True(allowed.IsAuthorized);
    }

    [Fact]
    public void DescriptorRegistryAggregatesModuleDescriptorsDeterministically()
    {
        var registry = new DefaultPermissionDescriptorRegistry(
        [
            new StaticDescriptorProvider(new PermissionDescriptor("reports:view", "View reports", "", "Reports")),
            new StaticDescriptorProvider(new PermissionDescriptor("workflows:read", "Read workflows", "", "Workflows"))
        ]);

        Assert.Equal(["reports:view", "workflows:read"], registry.List().Select(x => x.Name));
    }

    private static DefaultPermissionGrantResolver CreateResolver(IUserProvider userProvider, IRoleProvider roleProvider, ExternalAuthenticationOptions options) => new(
        [new ElsaRolePermissionGrantSource(userProvider, roleProvider), new ClaimMappingPermissionGrantSource(), new GroupMappingPermissionGrantSource(), new ClaimPassThroughPermissionGrantSource()],
        new DefaultPermissionDescriptorRegistry([]),
        Microsoft.Extensions.Options.Options.Create(options));

    private static PermissionGrantResolutionContext CreateContext(IReadOnlyCollection<GrantSourceSelection> selections, IReadOnlyDictionary<string, IReadOnlyCollection<string>> claims)
    {
        var connection = new IdentityProviderConnection { Id = "connection-a", TenantId = "tenant-a", Key = "contoso", AdapterType = "oidc", AdapterSettingsVersion = 1, DisplayName = "Contoso", PermissionGrantSources = selections.ToArray() };
        return new PermissionGrantResolutionContext("tenant-a", "user-a", new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Configuration, new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), ConnectionValidity.Valid, false, "test"), null, claims);
    }

    private static ClaimsPrincipal CreateActor(params string[] permissions) => new(new ClaimsIdentity(permissions.Select(x => new Claim(PermissionNames.ClaimType, x)), "test"));

    private sealed class StaticUserProvider(User? user) : IUserProvider
    {
        public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(user?.Id == filter.Id ? user : null);
    }

    private sealed class StaticRoleProvider(params Role[] roles) : IRoleProvider
    {
        public ValueTask<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default) => ValueTask.FromResult<IEnumerable<Role>>(roles.Where(x => filter.Ids?.Contains(x.Id) ?? true));
    }

    private sealed class StaticDescriptorProvider(params PermissionDescriptor[] descriptors) : IPermissionDescriptorProvider
    {
        public IEnumerable<PermissionDescriptor> GetDescriptors() => descriptors;
    }
}
