using Elsa.Permissions;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Authorization;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Permissions;

public class PermissionGrantPipelineTests
{
    [Test]
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

        await Assert.That(result.Grants.Select(x => x.Permission)).IsEquivalentTo(["workflows:manage", "workflows:read", "reports:view"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(result.Grants.First(x => x.Permission == "workflows:read").SourceType).IsEqualTo("elsa-roles");
        await Assert.That(result.Grants.First(x => x.Permission == "workflows:manage").SourceReference).IsEqualTo("role-a");
        await Assert.That(result.Grants.Last().SourceReference).IsEqualTo("department:engineering");
    }

    [Test]
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
        await Assert.That(unmapped.Grants).IsEmpty();

        var mappedContext = context with { ProjectedClaims = new Dictionary<string, IReadOnlyCollection<string>> { ["department"] = ["engineering"] } };
        var mapped = await resolver.ResolveAsync(mappedContext);

        await Assert.That(mapped.Grants.Select(x => x.Permission)).IsEquivalentTo(["reports:view"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(mapped.Warnings).Contains(warning => warning.Code == "permission_denied_by_deployment");
        await Assert.That(mapped.Warnings).Contains(warning => warning.Code == "unknown_permission_descriptor" && warning.Message.Contains("reports:view", StringComparison.Ordinal));
    }

    [Test]
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

        await Assert.That(result.Grants.Select(x => x.Permission)).IsEquivalentTo(["reports:view", "workflows:read"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(result.Warnings).HasSingleItem(warning => warning.Code == "unknown_permission_descriptor" && warning.Message.Contains("reports:view", StringComparison.Ordinal));
    }

    [Test]
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

        await Assert.That(result.Grants).IsEmpty();
        await Assert.That(result.Warnings).HasSingleItem(warning => warning.Code == "permission_denied_by_deployment");
    }

    [Test]
    public async Task PassThroughClaimsGrantNothingWithoutAnExplicitNonEmptyBoundary()
    {
        var resolver = CreateResolver(new StaticUserProvider(null), new StaticRoleProvider(), new ExternalAuthenticationOptions());
        var empty = CreateContext(
            [new GrantSourceSelection("claim-pass-through", 1, JsonSerializer.SerializeToElement(new { claimType = "permissions", allowedPermissions = Array.Empty<string>() }), 0)],
            new Dictionary<string, IReadOnlyCollection<string>> { ["permissions"] = ["reports:view", "workflows:manage"] });

        var emptyResult = await resolver.ResolveAsync(empty);
        await Assert.That(emptyResult.Grants).IsEmpty();

        var bounded = empty with
        {
            Connection = new EffectiveIdentityProviderConnection(new IdentityProviderConnection
            {
                Id = "connection-a", TenantId = "tenant-a", Key = "contoso", AdapterType = "oidc", AdapterSettingsVersion = 1, DisplayName = "Contoso",
                PermissionGrantSources = [new GrantSourceSelection("claim-pass-through", 1, JsonSerializer.SerializeToElement(new { claimType = "permissions", allowedPermissions = new[] { "reports:view" } }), 0)]
            }, ConnectionSourceOwnership.Configuration, new ConnectionScope(ConnectionScopeKind.Tenant, "tenant-a"), ConnectionValidity.Valid, false, "test")
        };
        var boundedResult = await resolver.ResolveAsync(bounded);

        await Assert.That(boundedResult.Grants.Select(x => x.Permission)).IsEquivalentTo(["reports:view"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task DelegationRequiresTheActorToPossessMappedPermissionsUnlessUnrestrictedAndStillHonorsDeploymentDeny()
    {
        var selection = new GrantSourceSelection("group-mapping", 1, JsonSerializer.SerializeToElement(new { claimType = "groups", mappings = new Dictionary<string, string[]> { ["operators"] = ["workflows:manage"] } }), 0);
        var options = new ExternalAuthenticationOptions();
        var authorizer = new DefaultPermissionDelegationAuthorizer(Microsoft.Extensions.Options.Options.Create(options), PermissionEvaluator.Shared);
        var ordinaryActor = CreateActor(DelegatePermission, "workflows:read");

        var ordinary = await authorizer.AuthorizeAsync(ordinaryActor, [selection]);

        await Assert.That(ordinary.IsAuthorized).IsFalse();
        await Assert.That(ordinary.UnauthorizedPermissions).IsEquivalentTo(["workflows:manage"], TUnit.Assertions.Enums.CollectionOrdering.Matching);

        options.PermissionGrants.DeniedPermissions = ["workflows:manage"];
        var unrestricted = await authorizer.AuthorizeAsync(CreateActor(DelegateUnrestrictedPermission), [selection]);

        await Assert.That(unrestricted.IsAuthorized).IsFalse();
        await Assert.That(unrestricted.UnauthorizedPermissions).IsEquivalentTo(["workflows:manage"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task DelegationRequiresTheActorToPossessEachExplicitPassThroughPermission()
    {
        var selection = new GrantSourceSelection("claim-pass-through", 1, JsonSerializer.SerializeToElement(new { claimType = "permissions", allowedPermissions = new[] { "reports:view" } }), 0);
        var authorizer = new DefaultPermissionDelegationAuthorizer(Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions()), PermissionEvaluator.Shared);

        var denied = await authorizer.AuthorizeAsync(CreateActor(DelegatePermission), [selection]);
        var allowed = await authorizer.AuthorizeAsync(CreateActor(DelegatePermission, "reports:view"), [selection]);

        await Assert.That(denied.IsAuthorized).IsFalse();
        await Assert.That(denied.UnauthorizedPermissions).IsEquivalentTo(["reports:view"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(allowed.IsAuthorized).IsTrue();
    }

    [Test]
    // A deny of a subtree reaches every permission beneath it, the case the ordinal boundary missed.
    [Arguments("workflows/*:delete", "workflows/definitions:delete")]
    // And a wildcard grant cannot outflank a deny spelled out by name.
    [Arguments("workflows/definitions:delete", "workflows/*:delete")]
    // A verb wildcard reaches in both directions too.
    [Arguments("workflows/definitions:*", "workflows/definitions:delete")]
    [Arguments("workflows/definitions:delete", "workflows/definitions:*")]
    public async Task DeploymentDenyAndGrantAreMatchedAsPatternsInBothDirections(string denied, string granted)
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.DeniedPermissions = [denied];
        var resolver = CreateResolver(new StaticUserProvider(null), new StaticRoleProvider(), options);

        var result = await resolver.ResolveAsync(MappedContext(granted));

        await Assert.That(result.Grants).IsEmpty();
        await Assert.That(result.Warnings).Contains(warning => warning.Code == "permission_denied_by_deployment");
    }

    [Test]
    // An allow entry must cover the whole grant, so a subtree admits the permissions beneath it...
    [Arguments("workflows/*:delete", "workflows/definitions:delete", true)]
    [Arguments("workflows/definitions:*", "workflows/definitions:delete", true)]
    // ...but a grant broader than anything allowed is refused rather than admitted for the overlap.
    [Arguments("workflows/definitions:delete", "workflows/*:delete", false)]
    [Arguments("workflows/definitions:delete", "workflows/definitions:*", false)]
    public async Task DeploymentAllowListCoversGrantsBeneathItButNotGrantsBeyondIt(string allowed, string granted, bool isAdmitted)
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.AllowedPermissions = [allowed];
        var resolver = CreateResolver(new StaticUserProvider(null), new StaticRoleProvider(), options);

        var result = await resolver.ResolveAsync(MappedContext(granted));

        await Assert.That(result.Grants.Select(x => x.Permission)).IsEquivalentTo(isAdmitted ? [granted] : Array.Empty<string>(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    // An allow list that parses to nothing must not read as "no allow list", which means unrestricted.
    [Arguments(new[] { "not a permission" }, new string[0])]
    // One bad entry among good ones is still a boundary the deployment cannot have meant.
    [Arguments(new[] { "workflows/*:delete", "external-authentication:connections:read" }, new string[0])]
    // A deny entry that does not parse would otherwise stop denying what it names, silently.
    [Arguments(new string[0], new[] { "external-authentication:connections:read" })]
    public async Task AGrantBoundaryThatDoesNotParseAdmitsNothing(string[] allowed, string[] denied)
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.AllowedPermissions = allowed;
        options.PermissionGrants.DeniedPermissions = denied;
        var resolver = CreateResolver(new StaticUserProvider(null), new StaticRoleProvider(), options);

        var result = await resolver.ResolveAsync(MappedContext("workflows/definitions:delete"));

        await Assert.That(result.Grants).IsEmpty();
        await Assert.That(result.Warnings).Contains(warning => warning.Code == "permission_denied_by_deployment");
    }

    [Test]
    public async Task RolePermissionsAreMatchedAgainstTheDenyBoundaryAsPatterns()
    {
        var options = new ExternalAuthenticationOptions();
        options.PermissionGrants.DeniedPermissions = ["workflows/*:delete"];
        var userProvider = new StaticUserProvider(new User { Id = "user-a", TenantId = "tenant-a", Roles = ["role-a"] });
        var roleProvider = new StaticRoleProvider(new Role { Id = "role-a", Name = "Operators", TenantId = "tenant-a", Permissions = ["workflows/definitions:delete", "workflows/definitions:view"] });
        var resolver = CreateResolver(userProvider, roleProvider, options);
        var context = CreateContext(
            [new GrantSourceSelection("elsa-roles", 1, JsonSerializer.SerializeToElement(new { }), 0)],
            new Dictionary<string, IReadOnlyCollection<string>>());

        var result = await resolver.ResolveAsync(context);

        await Assert.That(result.Grants.Select(x => x.Permission)).IsEquivalentTo(["workflows/definitions:view"], TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(result.Warnings).Contains(warning => warning.Code == "permission_denied_by_deployment");
    }

    [Test]
    public async Task GrantsThatAreNotWellFormedPermissionsAreDroppedRatherThanCarriedIntoAToken()
    {
        var resolver = CreateResolver(new StaticUserProvider(null), new StaticRoleProvider(), new ExternalAuthenticationOptions());

        var result = await resolver.ResolveAsync(MappedContext("external-authentication:connections:read"));

        await Assert.That(result.Grants).IsEmpty();
        await Assert.That(result.Warnings).Contains(warning => warning.Code == "malformed_permission");
    }

    [Test]
    // The actor must cover what they delegate, so a subtree grant delegates the permissions beneath it...
    [Arguments("workflows/*:delete", "workflows/definitions:delete", true)]
    // ...and holding one leaf does not let an actor delegate the whole subtree.
    [Arguments("workflows/definitions:delete", "workflows/*:delete", false)]
    [Arguments(PermissionNames.All, "workflows/*:delete", true)]
    public async Task DelegationMatchesTheActorsOwnGrantsAsPatterns(string held, string delegated, bool isAuthorized)
    {
        var authorizer = new DefaultPermissionDelegationAuthorizer(Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions()), PermissionEvaluator.Shared);
        var selection = new GrantSourceSelection("group-mapping", 1, JsonSerializer.SerializeToElement(new { claimType = "groups", mappings = new Dictionary<string, string[]> { ["operators"] = [delegated] } }), 0);

        var result = await authorizer.AuthorizeAsync(CreateActor(DelegatePermission, held), [selection]);

        await Assert.That(result.IsAuthorized).IsEqualTo(isAuthorized);
    }

    [Test]
    public async Task DelegationPermissionItselfIsHonouredThroughAWildcardGrant()
    {
        var authorizer = new DefaultPermissionDelegationAuthorizer(Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions()), PermissionEvaluator.Shared);
        var selection = new GrantSourceSelection("group-mapping", 1, JsonSerializer.SerializeToElement(new { claimType = "groups", mappings = new Dictionary<string, string[]> { ["operators"] = ["reports:view"] } }), 0);

        var subtree = await authorizer.AuthorizeAsync(CreateActor($"{ExternalAuthenticationResourcePermissions.PermissionGrants}:*", "reports:view"), [selection]);
        var without = await authorizer.AuthorizeAsync(CreateActor("reports:view"), [selection]);

        await Assert.That(subtree.IsAuthorized).IsTrue();
        await Assert.That(without.IsAuthorized).IsFalse();
    }

    private const string DelegatePermission = $"{ExternalAuthenticationResourcePermissions.PermissionGrants}:{ExternalAuthenticationVerbs.Delegate}";
    private const string DelegateUnrestrictedPermission = $"{ExternalAuthenticationResourcePermissions.PermissionGrants}:{ExternalAuthenticationVerbs.DelegateUnrestricted}";

    private static PermissionGrantResolutionContext MappedContext(string permission) => CreateContext(
        [new GrantSourceSelection("claim-mapping", 1, JsonSerializer.SerializeToElement(new { claimType = "department", mappings = new Dictionary<string, string[]> { ["engineering"] = [permission] } }), 0)],
        new Dictionary<string, IReadOnlyCollection<string>> { ["department"] = ["engineering"] });

    [Test]
    // A grant the catalog advertises, resource and verb both matching, is not warned about.
    [Arguments("reports:view", false)]
    // A verb the resource does not declare is a gap worth surfacing.
    [Arguments("reports:delete", true)]
    // So is a resource nothing advertises.
    [Arguments("nothing/here:view", true)]
    // A wildcard names a pattern rather than one resource, so there is no descriptor to look it up in.
    [Arguments("reports:*", false)]
    [Arguments("*", false)]
    public async Task UnknownDescriptorWarningTracksTheCoreCatalog(string permission, bool expectsWarning)
    {
        var resolver = new DefaultPermissionGrantResolver(
            [new ClaimMappingPermissionGrantSource()],
            new DefaultPermissionDescriptorRegistry([new StaticDescriptorProvider(new PermissionDescriptor("reports", [CoreVerbs.View], "Reports", "", "Reports"))]),
            Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions()));

        var result = await resolver.ResolveAsync(MappedContext(permission));

        await Assert.That(result.Warnings.Any(x => x.Code == "unknown_permission_descriptor")).IsEqualTo(expectsWarning);
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
