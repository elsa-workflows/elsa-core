using Elsa.Common.Models;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Multitenancy;

/// <summary>
/// Tests for multitenancy tenant ID normalization.
/// </summary>
public class MultitenancyTests(App app) : AppComponentTest(app)
{
    [Fact]
    public void DefaultTenant_ShouldUseEmptyStringAsId()
    {
        // Assert
        Assert.Equal(string.Empty, Tenant.DefaultTenantId);
        Assert.Equal(Tenant.DefaultTenantId, Tenant.Default.Id);
    }

    [Fact]
    public void NormalizeTenantId_WithNull_ShouldReturnEmptyString()
    {
        // Arrange
        string? tenantId = null;

        // Act
        var normalizedId = tenantId.NormalizeTenantId();

        // Assert
        Assert.Equal(Tenant.DefaultTenantId, normalizedId);
        Assert.Equal(string.Empty, normalizedId);
    }

    [Fact]
    public void NormalizeTenantId_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var tenantId = string.Empty;

        // Act
        var normalizedId = tenantId.NormalizeTenantId();

        // Assert
        Assert.Equal(Tenant.DefaultTenantId, normalizedId);
    }

    [Fact]
    public void NormalizeTenantId_WithValidTenantId_ShouldReturnSameValue()
    {
        // Arrange
        var tenantId = "tenant-123";

        // Act
        var normalizedId = tenantId.NormalizeTenantId();

        // Assert
        Assert.Equal("tenant-123", normalizedId);
    }

    [Fact]
    public async Task WorkflowDefinitionStore_ShouldWorkWithTenantNormalization()
    {
        // Arrange
        var store = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var filter = new WorkflowDefinitionFilter
        {
            IsSystem = false,
            VersionOptions = VersionOptions.Latest
        };

        // Act & Assert - Should not throw exceptions related to tenant ID handling
        var workflows = await store.FindManyAsync(filter);
        Assert.NotNull(workflows);
    }

    [Fact]
    public void TenantResolverContext_FindTenant_WithNull_ShouldNormalize()
    {
        // Arrange
        var defaultTenant = new Tenant { Id = Tenant.DefaultTenantId, Name = "Default" };
        var tenant1 = new Tenant { Id = "tenant1", Name = "Tenant 1" };
        var tenantsDictionary = new Dictionary<string, Tenant>
        {
            { defaultTenant.Id, defaultTenant },
            { tenant1.Id, tenant1 }
        };
        var context = new TenantResolverContext(tenantsDictionary, CancellationToken.None);

        // Act
        string? nullTenantId = null;
        var result = context.FindTenant(nullTenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Tenant.DefaultTenantId, result.Id);
    }

    [Fact]
    public void TenantResolverContext_FindTenant_WithEmptyString_ShouldFindDefaultTenant()
    {
        // Arrange
        var defaultTenant = new Tenant { Id = Tenant.DefaultTenantId, Name = "Default" };
        var tenantsDictionary = new Dictionary<string, Tenant>
        {
            { defaultTenant.Id, defaultTenant }
        };
        var context = new TenantResolverContext(tenantsDictionary, CancellationToken.None);

        // Act
        var result = context.FindTenant(string.Empty);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Tenant.DefaultTenantId, result.Id);
        Assert.Equal("Default", result.Name);
    }
}
