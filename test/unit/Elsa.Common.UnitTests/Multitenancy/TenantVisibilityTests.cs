using Elsa.Common.Entities;
using Elsa.Common.Multitenancy;

namespace Elsa.Common.UnitTests.Multitenancy;

/// <summary>
/// Mirrors EF <c>SetTenantIdFilter</c>: ambient match, <c>*</c>, and null-on-default-tenant.
/// </summary>
public class TenantVisibilityTests
{
    [Test]
    [Arguments("tenant-a", "tenant-a", true)]
    [Arguments("tenant-a", "tenant-b", false)]
    [Arguments(Tenant.AgnosticTenantId, "tenant-a", true)]
    [Arguments(Tenant.AgnosticTenantId, Tenant.DefaultTenantId, true)]
    [Arguments(null, Tenant.DefaultTenantId, true)]
    [Arguments(null, "tenant-a", false)]
    [Arguments(Tenant.DefaultTenantId, Tenant.DefaultTenantId, true)]
    [Arguments(Tenant.DefaultTenantId, "tenant-a", false)]
    public async Task IsVisible_MatchesSetTenantIdFilter(string? entityTenantId, string ambientTenantId, bool expected)
    {
        await Assert.That(TenantVisibility.IsVisible(entityTenantId, ambientTenantId)).IsEqualTo(expected);
    }

    [Test]
    public async Task WhereVisibleToTenant_WhenNotAgnostic_HidesOtherTenants()
    {
        var queryable = new[]
        {
            Entity("tenant-a"),
            Entity("tenant-b"),
            Entity(Tenant.AgnosticTenantId),
            Entity(null)
        }.AsQueryable();

        var visible = queryable.WhereVisibleToTenant("tenant-a").Select(x => x.TenantId).ToList();

        await Assert.That(visible.Count).IsEqualTo(2);
        await Assert.That(visible).Contains("tenant-a");
        await Assert.That(visible).Contains(Tenant.AgnosticTenantId);
    }

    [Test]
    public async Task WhereVisibleToTenant_WhenAgnostic_ReturnsAllRows()
    {
        var queryable = new[]
        {
            Entity("tenant-a"),
            Entity("tenant-b")
        }.AsQueryable();

        var visible = queryable.WhereVisibleToTenant("tenant-a", tenantAgnostic: true).ToList();

        await Assert.That(visible.Count).IsEqualTo(2);
    }

    [Test]
    [Arguments("tenant-a", "tenant-a", true)]
    [Arguments("tenant-a", "tenant-b", false)]
    [Arguments(Tenant.AgnosticTenantId, Tenant.AgnosticTenantId, true)]
    [Arguments(Tenant.AgnosticTenantId, "tenant-a", false)]
    [Arguments(Tenant.AgnosticTenantId, Tenant.DefaultTenantId, false)]
    [Arguments(null, Tenant.DefaultTenantId, true)]
    [Arguments(null, "tenant-a", false)]
    [Arguments(Tenant.DefaultTenantId, Tenant.DefaultTenantId, true)]
    public async Task CanReplace_MatchesMemoryAlterationOwnership(string? existingTenantId, string writerTenantId, bool expected)
    {
        await Assert.That(TenantVisibility.CanReplace(existingTenantId, writerTenantId)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("tenant-a", "tenant-a", "tenant-a", true)]
    [Arguments("tenant-a", "tenant-b", "tenant-b", false)]
    [Arguments("tenant-a", "tenant-a", "tenant-b", false)]
    [Arguments(Tenant.AgnosticTenantId, Tenant.AgnosticTenantId, Tenant.AgnosticTenantId, true)]
    [Arguments(Tenant.AgnosticTenantId, Tenant.AgnosticTenantId, "tenant-a", false)]
    [Arguments(Tenant.AgnosticTenantId, "tenant-a", "tenant-a", false)]
    [Arguments(Tenant.AgnosticTenantId, "tenant-a", Tenant.AgnosticTenantId, false)]
    [Arguments(null, Tenant.DefaultTenantId, Tenant.DefaultTenantId, true)]
    [Arguments(null, "tenant-a", "tenant-a", false)]
    [Arguments(null, Tenant.DefaultTenantId, "tenant-a", false)]
    public async Task CanReplaceOwnedRow_GatesNamedRowsOnAmbientNotForgedSource(
        string? existingTenantId,
        string? sourceTenantId,
        string ambientTenantId,
        bool expected)
    {
        await Assert.That(TenantVisibility.CanReplaceOwnedRow(existingTenantId, sourceTenantId, ambientTenantId)).IsEqualTo(expected);
    }

    [Test]
    public async Task WhereVisibleToTenant_WhenAmbientIsDefault_IncludesNullTenantId()
    {
        var queryable = new[]
        {
            Entity(null),
            Entity("tenant-a")
        }.AsQueryable();

        var visible = queryable.WhereVisibleToTenant(Tenant.DefaultTenantId).ToList();

        await Assert.That(visible).HasSingleItem();
        await Assert.That(visible[0].TenantId).IsNull();
    }

    private static TestEntity Entity(string? tenantId) => new()
    {
        Id = tenantId ?? "null",
        TenantId = tenantId
    };

    private sealed class TestEntity : Entity;
}
