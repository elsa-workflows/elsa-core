using AspNetCore.Authentication.ApiKey;
using Elsa;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Options;
using Elsa.Identity.Providers;
using Elsa.Identity.Services;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Providers;

public class DefaultApiKeyProviderTests
{
    [Fact]
    public async Task ContradictoryTenantIsRejectedBeforeUpdatingAConfiguredLegacyKey()
    {
        var accessor = new DefaultTenantAccessor();
        var application = new Application { ClientId = "app", Name = "App", HashedApiKey = "old-hash", HashedApiKeySalt = "old-salt" };
        var applications = new ConfigurationBasedApplicationProvider(Microsoft.Extensions.Options.Options.Create(new ApplicationsOptions { Applications = [application] }));
        var store = Substitute.For<IApplicationStore>();
        var hasher = Substitute.For<ISecretHasher>();
        var parser = Substitute.For<IApiKeyParser>();
        parser.Parse("configured-key").Returns("app");
        var validator = new DefaultApplicationCredentialsValidator(parser, applications, store, hasher, null, accessor);

        using var scope = accessor.PushContext(new Tenant { Id = "tenant-b", Name = "Tenant B" });

        Assert.Null(await validator.ValidateAsync("configured-key"));
        hasher.DidNotReceiveWithAnyArgs().VerifySecret(default!, default!, default!, out _);
        await store.DidNotReceiveWithAnyArgs().SaveAsync(default!);
        Assert.Null(application.TenantId);
        Assert.Equal("old-hash", application.HashedApiKey);
    }

    [Fact]
    public async Task ConfiguredApplicationKeyBindsTenantAndResolvesRolesWithinIt()
    {
        var accessor = new DefaultTenantAccessor();
        var roleStore = new MemoryRoleStore(new MemoryStore<Role>(), accessor);
        await roleStore.SaveAsync(new Role { Id = "own", Name = "Own", TenantId = "tenant-a", Permissions = ["workflows/definitions:view"] });
        await roleStore.SaveAsync(new Role { Id = "other", Name = "Other", TenantId = "tenant-b", Permissions = [PermissionNames.All] });
        var applications = new ConfigurationBasedApplicationProvider(Microsoft.Extensions.Options.Options.Create(new ApplicationsOptions
        {
            Applications = [new Application { ClientId = "app", TenantId = "tenant-a", Roles = ["own", "other"], HashedApiKey = "hash", HashedApiKeySalt = "salt" }]
        }));
        var hints = Substitute.For<ITenantRequestHints>();
        hints.GetAsync().Returns(Array.Empty<string>());
        var provider = CreateProvider(accessor, applications, roleStore, hints);

        var key = Assert.IsAssignableFrom<IApiKey>(await provider.ProvideAsync("configured-key"));

        Assert.Contains(key.Claims, claim => claim.Type == TenantPrincipalClaims.BoundTenant && claim.Value == "tenant-a");
        Assert.Contains(key.Claims, claim => claim.Type == CustomClaimTypes.TenantId && claim.Value == "tenant-a");
        Assert.Equal("workflows/definitions:view", Assert.Single(key.Claims, claim => claim.Type == PermissionNames.ClaimType).Value);
        Assert.Null(accessor.Tenant);

        hints.GetAsync().Returns(new[] { "tenant-b" });
        Assert.Null(await provider.ProvideAsync("configured-key"));
        hints.GetAsync().Returns(new[] { "tenant-a", "tenant-b" });
        Assert.Null(await provider.ProvideAsync("configured-key"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("tenant-a")]
    [InlineData("tenant-b")]
    public async Task StoredApplicationKeyAuthenticatesInItsRegisteredTenant(string tenantId)
    {
        var accessor = new DefaultTenantAccessor();
        var apps = new MemoryApplicationStore(new MemoryStore<Application>(), accessor);
        var roles = new MemoryRoleStore(new MemoryStore<Role>(), accessor);
        await apps.SaveAsync(new Application { Id = "application-id", ClientId = "app", Name = "App", TenantId = tenantId, HashedApiKey = "hash", HashedApiKeySalt = "salt", Roles = ["reader"] });
        await roles.SaveAsync(new Role { Id = "reader", Name = "Reader", TenantId = tenantId, Permissions = ["workflows/definitions:view"] });
        var hints = Substitute.For<ITenantRequestHints>();
        hints.GetAsync().Returns(tenantId == Tenant.DefaultTenantId ? [] : new[] { tenantId });
        var provider = CreateProvider(accessor, new StoreBasedApplicationProvider(apps), roles, hints);

        var key = Assert.IsAssignableFrom<IApiKey>(await provider.ProvideAsync("configured-key"));

        Assert.Contains(key.Claims, claim => claim.Type == TenantPrincipalClaims.BoundTenant && claim.Value == tenantId);
        Assert.Equal("workflows/definitions:view", Assert.Single(key.Claims, claim => claim.Type == PermissionNames.ClaimType).Value);
        hints.GetAsync().Returns(new[] { "missing" });
        Assert.Null(await provider.ProvideAsync("configured-key"));
    }

    [Fact]
    public async Task AZeroGrantApplicationGetsTheEmptySetSentinel()
    {
        var apiKey = await ProvideAsync("noperm", permissions: []);

        Assert.NotNull(apiKey);
        Assert.Equal("client-noperm", apiKey.OwnerName);
        Assert.Equal(PermissionNames.None, Assert.Single(apiKey.Claims, x => x.Type == PermissionNames.ClaimType).Value);
    }

    [Fact]
    public async Task AGrantedApplicationKeepsItsPermissions()
    {
        var apiKey = await ProvideAsync("reader", permissions: ["identity/users:view", "identity/roles:view"]);

        Assert.NotNull(apiKey);
        Assert.Equal(
            new[] { "identity/users:view", "identity/roles:view" },
            apiKey.Claims.Where(x => x.Type == PermissionNames.ClaimType).Select(x => x.Value).ToArray());
        Assert.DoesNotContain(apiKey.Claims, x => x.Type == PermissionNames.ClaimType && x.Value == PermissionNames.None);
    }

    private static async Task<IApiKey?> ProvideAsync(string roleId, IReadOnlyCollection<string> permissions)
    {
        var application = new Application { ClientId = $"client-{roleId}", Roles = [roleId] };
        var validator = Substitute.For<IApplicationCredentialsValidator>();
        validator.ValidateAsync("key", Arg.Any<CancellationToken>()).Returns(application);

        var roles = Substitute.For<IRoleProvider>();
        roles.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>())
            .Returns([new Role { Id = roleId, Name = roleId, Permissions = permissions.ToList() }]);

        return await new DefaultApiKeyProvider(validator, roles).ProvideAsync("key");
    }

    private static DefaultApiKeyProvider CreateProvider(ITenantAccessor accessor, IApplicationProvider applications, IRoleStore roles, ITenantRequestHints hints)
    {
        var parser = Substitute.For<IApiKeyParser>();
        parser.Parse("configured-key").Returns("app");
        var hasher = Substitute.For<ISecretHasher>();
        hasher.VerifySecret("configured-key", "hash", "salt", out _).Returns(true);
        var tenants = Substitute.For<ITenantService>();
        tenants.FindAsync(Tenant.DefaultTenantId, Arg.Any<CancellationToken>()).Returns(Tenant.Default);
        tenants.FindAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(new Tenant { Id = "tenant-a", Name = "Tenant A" });
        tenants.FindAsync("tenant-b", Arg.Any<CancellationToken>()).Returns(new Tenant { Id = "tenant-b", Name = "Tenant B" });
        var validator = new DefaultApplicationCredentialsValidator(parser, applications, null, hasher, null, accessor, tenants);
        return new(validator, new StoreBasedRoleProvider(roles), accessor,
            Microsoft.Extensions.Options.Options.Create(new IdentityTokenOptions()), tenants, hints);
    }
}
