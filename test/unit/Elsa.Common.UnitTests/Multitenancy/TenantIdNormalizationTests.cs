using Elsa.Common.Multitenancy;
using System.Threading.Tasks;

namespace Elsa.Common.UnitTests.Multitenancy;

public class TenantIdNormalizationTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task NormalizeTenantId_WithNullOrEmpty_ReturnsDefaultTenantId(string? tenantId)
    {
        // Act
        var result = tenantId.NormalizeTenantId();

        // Assert
        await Assert.That(result).IsEqualTo(Tenant.DefaultTenantId);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments("tenant1")]
    [Arguments("tenant-abc-123")]
    [Arguments("DEFAULT")]
    [Arguments("my-custom-tenant")]
    [Arguments(" ")] // Whitespace is not normalized
    public async Task NormalizeTenantId_WithNonNullString_ReturnsOriginalValue(string tenantId)
    {
        // Act
        var result = tenantId.NormalizeTenantId();

        // Assert
        await Assert.That(result).IsEqualTo(tenantId);
    }

    [Test]
    public async Task DefaultTenantId_IsEmptyString()
    {
        // Assert
        await Assert.That(string.Empty).IsEqualTo(Tenant.DefaultTenantId);
    }

    [Test]
    public async Task DefaultTenant_UsesDefaultTenantId()
    {
        // Assert
        await Assert.That(Tenant.Default.Id).IsEqualTo(Tenant.DefaultTenantId);
        await Assert.That(Tenant.Default.Id).IsEqualTo(string.Empty);
    }
}