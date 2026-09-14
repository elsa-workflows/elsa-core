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
    [Test]
    public async Task DefaultTenant_ShouldUseEmptyStringAsId()
    {
        // Assert
        await Assert.That(Tenant.DefaultTenantId).IsEmpty();
        await Assert.That(Tenant.Default.Id).IsEqualTo(Tenant.DefaultTenantId);
    }

    [Test]
    public async Task NormalizeTenantId_WithNull_ShouldReturnEmptyString()
    {
        // Arrange
        string? tenantId = null;

        // Act
        var normalizedId = tenantId.NormalizeTenantId();

        // Assert
        await Assert.That(normalizedId).IsEqualTo(Tenant.DefaultTenantId);
        await Assert.That(normalizedId).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task NormalizeTenantId_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var tenantId = string.Empty;

        // Act
        var normalizedId = tenantId.NormalizeTenantId();

        // Assert
        await Assert.That(normalizedId).IsEqualTo(Tenant.DefaultTenantId);
    }

    [Test]
    public async Task NormalizeTenantId_WithValidTenantId_ShouldReturnSameValue()
    {
        // Arrange
        var tenantId = "tenant-123";

        // Act
        var normalizedId = tenantId.NormalizeTenantId();

        // Assert
        await Assert.That(normalizedId).IsEqualTo("tenant-123");
    }

    [Test]
    public async Task WorkflowDefinitionStore_ShouldWorkWithTenantNormalization()
    {
        // Arrange
        var store = Scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionStore>();
        var tenantsProvider = Scope.ServiceProvider.GetRequiredService<ITenantsProvider>();
        var filter = new WorkflowDefinitionFilter
        {
            IsSystem = false,
            VersionOptions = VersionOptions.Latest
        };

        // Act & Assert - Should not throw exceptions related to tenant ID handling
        var workflows = await store.FindManyAsync(filter);
        var tenants = (await tenantsProvider.ListAsync()).ToArray();
        var tenant2 = await tenantsProvider.FindAsync(TenantFilter.ById("Tenant2"));

        await Assert.That(workflows).IsNotNull();
        await Assert.That(tenants.Select(x => x.Id)).IsEquivalentTo([string.Empty, "Tenant1", "Tenant2", "Tenant3"]);
        await Assert.That(tenant2).IsNotNull();
        await Assert.That(tenant2.Id).IsEqualTo("Tenant2");
        await Assert.That(tenant2.Name).IsEqualTo("Tenant2");
    }

    [Test]
    public async Task TenantResolverContext_FindTenant_WithNull_ShouldNormalize()
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
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(Tenant.DefaultTenantId);
    }

    [Test]
    public async Task TenantResolverContext_FindTenant_WithEmptyString_ShouldFindDefaultTenant()
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
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(Tenant.DefaultTenantId);
        await Assert.That(result.Name).IsEqualTo("Default");
    }
}
