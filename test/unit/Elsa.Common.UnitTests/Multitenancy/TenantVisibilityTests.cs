using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;

namespace Elsa.Common.UnitTests.Multitenancy;

/// <summary>
/// Mirrors EF <c>SetTenantIdFilter</c>: ambient match, <c>*</c>, and null-on-default-tenant.
/// </summary>
public class TenantVisibilityTests
{
    [Theory]
    [InlineData("tenant-a", "tenant-a", true)]
    [InlineData("tenant-a", "tenant-b", false)]
    [InlineData(Tenant.AgnosticTenantId, "tenant-a", true)]
    [InlineData(Tenant.AgnosticTenantId, Tenant.DefaultTenantId, true)]
    [InlineData(null, Tenant.DefaultTenantId, true)]
    [InlineData(null, "tenant-a", false)]
    [InlineData(Tenant.DefaultTenantId, Tenant.DefaultTenantId, true)]
    [InlineData(Tenant.DefaultTenantId, "tenant-a", false)]
    public void IsVisible_MatchesSetTenantIdFilter(string? entityTenantId, string ambientTenantId, bool expected)
    {
        Assert.Equal(expected, TenantVisibility.IsVisible(entityTenantId, ambientTenantId));
    }

    [Fact]
    public void WhereVisibleToTenant_WhenNotAgnostic_HidesOtherTenants()
    {
        var queryable = new[]
        {
            Entity("tenant-a"),
            Entity("tenant-b"),
            Entity(Tenant.AgnosticTenantId),
            Entity(null)
        }.AsQueryable();

        var visible = queryable.WhereVisibleToTenant("tenant-a").Select(x => x.TenantId).ToList();

        Assert.Equal(2, visible.Count);
        Assert.Contains("tenant-a", visible);
        Assert.Contains(Tenant.AgnosticTenantId, visible);
    }

    [Fact]
    public void WhereVisibleToTenant_WhenAgnostic_ReturnsAllRows()
    {
        var queryable = new[]
        {
            Entity("tenant-a"),
            Entity("tenant-b")
        }.AsQueryable();

        var visible = queryable.WhereVisibleToTenant("tenant-a", tenantAgnostic: true).ToList();

        Assert.Equal(2, visible.Count);
    }

    [Fact]
    public void WhereVisibleToTenant_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var queryable = new[]
        {
            Entity(null),
            Entity("tenant-a")
        }.AsQueryable();

        var visible = queryable.WhereVisibleToTenant(Tenant.DefaultTenantId).ToList();

        Assert.Single(visible);
        Assert.Null(visible[0].TenantId);
    }

    private static TestEntity Entity(string? tenantId) => new()
    {
        Id = tenantId ?? "null",
        TenantId = tenantId
    };

    private sealed class TestEntity : Entity;
}
