using Elsa.Common.Multitenancy;

namespace Elsa.Common.UnitTests.Multitenancy;

public class TenantIdNormalizationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NormalizeTenantId_WithNullOrEmpty_ReturnsDefaultTenantId(string? tenantId)
    {
        // Act
        var result = tenantId.NormalizeTenantId();

        // Assert
        Assert.Equal(Tenant.DefaultTenantId, result);
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("tenant1")]
    [InlineData("tenant-abc-123")]
    [InlineData("DEFAULT")]
    [InlineData("my-custom-tenant")]
    [InlineData(" ")] // Whitespace is not normalized
    public void NormalizeTenantId_WithNonNullString_ReturnsOriginalValue(string tenantId)
    {
        // Act
        var result = tenantId.NormalizeTenantId();

        // Assert
        Assert.Equal(tenantId, result);
    }

    [Fact]
    public void DefaultTenantId_IsEmptyString()
    {
        // Assert
        Assert.Equal(Tenant.DefaultTenantId, string.Empty);
    }

    [Fact]
    public void DefaultTenant_UsesDefaultTenantId()
    {
        // Assert
        Assert.Equal(Tenant.DefaultTenantId, Tenant.Default.Id);
        Assert.Equal(string.Empty, Tenant.Default.Id);
    }
}
