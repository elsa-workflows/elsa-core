using Elsa.Common.Multitenancy;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Services;

/// <summary>
/// Unit tests for ActivityRegistry, specifically testing multi-tenant descriptor resolution logic.
/// </summary>
public class ActivityRegistryTests
{
    private const string TestActivityType = "TestActivity";
    private const string CurrentTenant = "tenant1";

    private readonly ITenantAccessor _tenantAccessor;
    private readonly IActivityDescriber _activityDescriber;
    private readonly ILogger<ActivityRegistry> _logger;
    private readonly ActivityRegistry _registry;

    public ActivityRegistryTests()
    {
        _tenantAccessor = Substitute.For<ITenantAccessor>();
        _activityDescriber = Substitute.For<IActivityDescriber>();
        _logger = Substitute.For<ILogger<ActivityRegistry>>();
        _registry = new ActivityRegistry(_activityDescriber, Array.Empty<IActivityDescriptorModifier>(), _tenantAccessor, _logger);
        
        // Set default tenant for all tests
        _tenantAccessor.TenantId.Returns(CurrentTenant);
    }

    private ActivityDescriptor CreateDescriptor(string typeName, int version, string? tenantId) =>
        new()
        {
            TypeName = typeName,
            Version = version,
            TenantId = tenantId,
            Kind = ActivityKind.Action
        };

    private void RegisterDescriptors(params ActivityDescriptor[] descriptors)
    {
        foreach (var descriptor in descriptors)
            _registry.Register(descriptor);
    }

    private static void AssertDescriptor(ActivityDescriptor? result, string? expectedTenantId, int expectedVersion)
    {
        Assert.NotNull(result);
        Assert.Equal(expectedTenantId, result.TenantId);
        Assert.Equal(expectedVersion, result.Version);
    }

    [Fact]
    public void Find_TenantSpecificPreferredOverTenantAgnostic_WhenBothExist()
    {
        // Arrange
        var tenantSpecific = CreateDescriptor(TestActivityType, 1, CurrentTenant);
        var tenantAgnostic = CreateDescriptor(TestActivityType, 2, Tenant.AgnosticTenantId); // Higher version
        RegisterDescriptors(tenantSpecific, tenantAgnostic);

        // Act
        var result = _registry.Find(TestActivityType);

        // Assert - tenant-specific should be preferred even though it has a lower version
        AssertDescriptor(result, CurrentTenant, 1);
    }

    [Fact]
    public void Find_ReturnsTenantAgnostic_WhenNoTenantSpecificExists()
    {
        // Arrange
        var tenantAgnostic = CreateDescriptor(TestActivityType, 1, Tenant.AgnosticTenantId);
        RegisterDescriptors(tenantAgnostic);

        // Act
        var result = _registry.Find(TestActivityType);

        // Assert
        AssertDescriptor(result, Tenant.AgnosticTenantId, 1);
    }

    [Theory]
    [InlineData(1, 2, 3, 3)] // Multiple versions, expect highest
    [InlineData(3, 1, 2, 3)] // Out of order registration
    [InlineData(1, 1, 1, 1)] // Same version multiple times
    public void Find_ReturnsHighestVersionTenantSpecific_WhenMultipleTenantSpecificExist(int v1, int v2, int v3, int expectedVersion)
    {
        // Arrange
        var descriptors = new[]
        {
            CreateDescriptor(TestActivityType, v1, CurrentTenant),
            CreateDescriptor(TestActivityType, v2, CurrentTenant),
            CreateDescriptor(TestActivityType, v3, CurrentTenant)
        };
        RegisterDescriptors(descriptors);

        // Act
        var result = _registry.Find(TestActivityType);

        // Assert
        AssertDescriptor(result, CurrentTenant, expectedVersion);
    }

    [Theory]
    [InlineData(1, 2, 3, 3)] // Multiple versions, expect highest
    [InlineData(3, 1, 2, 3)] // Out of order registration
    [InlineData(1, 1, 1, 1)] // Same version multiple times
    public void Find_ReturnsHighestVersionTenantAgnostic_WhenMultipleTenantAgnosticExist(int v1, int v2, int v3, int expectedVersion)
    {
        // Arrange
        var descriptors = new[]
        {
            CreateDescriptor(TestActivityType, v1, Tenant.AgnosticTenantId),
            CreateDescriptor(TestActivityType, v2, Tenant.AgnosticTenantId),
            CreateDescriptor(TestActivityType, v3, Tenant.AgnosticTenantId)
        };
        RegisterDescriptors(descriptors);

        // Act
        var result = _registry.Find(TestActivityType);

        // Assert
        AssertDescriptor(result, Tenant.AgnosticTenantId, expectedVersion);
    }

    [Fact]
    public void Find_ReturnsNull_WhenNoMatchingDescriptorsExist()
    {
        // Arrange
        var otherDescriptor = CreateDescriptor("OtherActivity", 1, CurrentTenant);
        RegisterDescriptors(otherDescriptor);

        // Act
        var result = _registry.Find("NonExistentActivity");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Find_IgnoresOtherTenantDescriptors_OnlyReturnsCurrentTenantOrAgnostic()
    {
        // Arrange
        var descriptors = new[]
        {
            CreateDescriptor(TestActivityType, 1, CurrentTenant),
            CreateDescriptor(TestActivityType, 5, "tenant2"), // Much higher version but wrong tenant
            CreateDescriptor(TestActivityType, 2, Tenant.AgnosticTenantId)
        };
        RegisterDescriptors(descriptors);

        // Act
        var result = _registry.Find(TestActivityType);

        // Assert - should return tenant1 descriptor (not tenant2, even though it has higher version)
        AssertDescriptor(result, CurrentTenant, 1);
    }
}
