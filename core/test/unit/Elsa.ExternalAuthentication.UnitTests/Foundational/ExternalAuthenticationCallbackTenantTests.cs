using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Constants;
using Elsa.ExternalAuthentication.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationCallbackTenantTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task TryResolveAsync_WhenTenantIdIsMissing_PushesTheDefaultTenant(string? tenantId)
    {
        var accessor = new DefaultTenantAccessor();
        var defaultTenant = new Tenant
        {
            Id = Tenant.DefaultTenantId,
            Name = "Default",
            Configuration = Configuration("Default-Database")
        };
        var helper = CreateHelper(accessor, new MapTenantFinder(defaultTenant));

        var state = helper.Protect(tenantId, "nonce-1", TimeSpan.FromMinutes(10));
        using var opened = (await helper.TryResolveAsync(state))!.Push();

        Assert.NotNull(opened);
        Assert.Equal(Tenant.DefaultTenantId, opened.TenantId);
        Assert.Equal("nonce-1", opened.Nonce);
        Assert.Same(defaultTenant, opened.Tenant);
        Assert.Same(defaultTenant, accessor.Tenant);
        Assert.Equal("Default-Database", accessor.Tenant.Configuration.GetConnectionString("SqlServer"));
    }

    [Fact]
    public async Task TryResolveAsync_WhenTenantIdIsNamed_PushesTheCatalogTenant()
    {
        var accessor = new DefaultTenantAccessor();
        var acme = TenantWithDatabase("acme", "Tenant-Acme-Database");
        var helper = CreateHelper(accessor, new MapTenantFinder(acme));

        var state = helper.Protect("acme", "nonce-2", TimeSpan.FromMinutes(10));
        using var opened = (await helper.TryResolveAsync(state))!.Push();

        Assert.NotNull(opened);
        Assert.Equal("acme", opened.TenantId);
        Assert.Same(acme, accessor.Tenant);
        Assert.Equal("Tenant-Acme-Database", accessor.Tenant!.Configuration.GetConnectionString("SqlServer"));
    }

    [Fact]
    public async Task TryResolveAsync_WhenStateIsTampered_ReturnsNullAndLeavesAmbientTenantUnchanged()
    {
        var accessor = new DefaultTenantAccessor();
        var helper = CreateHelper(accessor, new MapTenantFinder(TenantWithDatabase("acme", "Tenant-Acme-Database")));
        var state = helper.Protect("acme", "nonce-3", TimeSpan.FromMinutes(10));
        var tampered = state[..^2] + (state[^1] == 'A' ? "B" : "A");

        var resolved = await helper.TryResolveAsync(tampered);

        Assert.Null(resolved);
        Assert.Null(accessor.Tenant);
    }

    [Fact]
    public async Task TryResolveAsync_WhenNamedTenantIsUnknown_ReturnsNullAndDoesNotFallBackToTheDefaultTenant()
    {
        var accessor = new DefaultTenantAccessor();
        var helper = CreateHelper(accessor, new MapTenantFinder());

        var state = helper.Protect("missing-tenant", "nonce-4", TimeSpan.FromMinutes(10));
        var resolved = await helper.TryResolveAsync(state);

        Assert.Null(resolved);
        Assert.Null(accessor.Tenant);
    }

    [Fact]
    public void Protect_UsesTheCallbackStatePurpose()
    {
        var provider = new EphemeralDataProtectionProvider();
        var helper = new ExternalAuthenticationCallbackTenant(provider, new MapTenantFinder(), new DefaultTenantAccessor());
        var state = helper.Protect("acme", "nonce-5", TimeSpan.FromMinutes(10));
        var payload = provider.CreateProtector(ExternalAuthenticationDataProtectionPurposes.CallbackState)
            .ToTimeLimitedDataProtector()
            .Unprotect(state);
        Assert.Equal("acme\nnonce-5", payload);
    }

    [Theory]
    [InlineData("acme", "acme", true)]
    [InlineData("", null, true)]
    [InlineData("", "", true)]
    [InlineData("acme", "other", false)]
    [InlineData("acme", "", false)]
    public void MatchesStoredTenantId_NormalizesTheDefaultTenant(string stateTenantId, string? storedTenantId, bool expected) =>
        Assert.Equal(expected, CreateHelper(new DefaultTenantAccessor(), new MapTenantFinder()).MatchesStoredTenantId(stateTenantId, storedTenantId));

    private static ExternalAuthenticationCallbackTenant CreateHelper(ITenantAccessor accessor, ITenantFinder finder) =>
        new(new EphemeralDataProtectionProvider(), finder, accessor);

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

    private sealed class MapTenantFinder(params Tenant[] tenants) : ITenantFinder
    {
        public Task<Tenant?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            tenantId = string.IsNullOrEmpty(tenantId) ? Tenant.DefaultTenantId : tenantId;
            return Task.FromResult(tenants.FirstOrDefault(tenant => tenant.Id == tenantId));
        }
    }
}
