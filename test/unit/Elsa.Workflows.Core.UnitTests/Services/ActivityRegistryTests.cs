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
    }

    [Fact]
    public void Find_TenantSpecificPreferredOverTenantAgnostic_WhenBothExist()
    {
        // Arrange - tenant1 is the current tenant
        _tenantAccessor.TenantId.Returns("tenant1");

        var tenantSpecificDescriptor = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 1,
            TenantId = "tenant1",
            Kind = ActivityKind.Action
        };

        var tenantAgnosticDescriptor = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 2, // Higher version
            TenantId = null,
            Kind = ActivityKind.Action
        };

        _registry.Register(tenantSpecificDescriptor);
        _registry.Register(tenantAgnosticDescriptor);

        // Act
        var result = _registry.Find("TestActivity");

        // Assert - tenant-specific should be preferred even though it has a lower version
        Assert.NotNull(result);
        Assert.Equal("tenant1", result.TenantId);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public void Find_ReturnsTenantAgnostic_WhenNoTenantSpecificExists()
    {
        // Arrange - tenant1 is the current tenant
        _tenantAccessor.TenantId.Returns("tenant1");

        var tenantAgnosticDescriptor = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 1,
            TenantId = null,
            Kind = ActivityKind.Action
        };

        _registry.Register(tenantAgnosticDescriptor);

        // Act
        var result = _registry.Find("TestActivity");

        // Assert - tenant-agnostic should be returned when no tenant-specific exists
        Assert.NotNull(result);
        Assert.Null(result.TenantId);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public void Find_ReturnsHighestVersionTenantSpecific_WhenMultipleTenantSpecificExist()
    {
        // Arrange - tenant1 is the current tenant
        _tenantAccessor.TenantId.Returns("tenant1");

        var tenantSpecificV1 = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 1,
            TenantId = "tenant1",
            Kind = ActivityKind.Action
        };

        var tenantSpecificV2 = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 2,
            TenantId = "tenant1",
            Kind = ActivityKind.Action
        };

        var tenantSpecificV3 = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 3,
            TenantId = "tenant1",
            Kind = ActivityKind.Action
        };

        _registry.Register(tenantSpecificV1);
        _registry.Register(tenantSpecificV2);
        _registry.Register(tenantSpecificV3);

        // Act
        var result = _registry.Find("TestActivity");

        // Assert - highest version tenant-specific should be returned
        Assert.NotNull(result);
        Assert.Equal("tenant1", result.TenantId);
        Assert.Equal(3, result.Version);
    }

    [Fact]
    public void Find_ReturnsHighestVersionTenantAgnostic_WhenMultipleTenantAgnosticExist()
    {
        // Arrange - tenant1 is the current tenant
        _tenantAccessor.TenantId.Returns("tenant1");

        var tenantAgnosticV1 = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 1,
            TenantId = null,
            Kind = ActivityKind.Action
        };

        var tenantAgnosticV2 = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 2,
            TenantId = null,
            Kind = ActivityKind.Action
        };

        var tenantAgnosticV3 = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 3,
            TenantId = null,
            Kind = ActivityKind.Action
        };

        _registry.Register(tenantAgnosticV1);
        _registry.Register(tenantAgnosticV2);
        _registry.Register(tenantAgnosticV3);

        // Act
        var result = _registry.Find("TestActivity");

        // Assert - highest version tenant-agnostic should be returned
        Assert.NotNull(result);
        Assert.Null(result.TenantId);
        Assert.Equal(3, result.Version);
    }

    [Fact]
    public void Find_ReturnsNull_WhenNoMatchingDescriptorsExist()
    {
        // Arrange - tenant1 is the current tenant
        _tenantAccessor.TenantId.Returns("tenant1");

        var otherDescriptor = new ActivityDescriptor
        {
            TypeName = "OtherActivity",
            Version = 1,
            TenantId = "tenant1",
            Kind = ActivityKind.Action
        };

        _registry.Register(otherDescriptor);

        // Act
        var result = _registry.Find("NonExistentActivity");

        // Assert - null should be returned when no matching descriptors exist
        Assert.Null(result);
    }

    [Fact]
    public void Find_IgnoresOtherTenantDescriptors_OnlyReturnsCurrentTenantOrAgnostic()
    {
        // Arrange - tenant1 is the current tenant
        _tenantAccessor.TenantId.Returns("tenant1");

        var tenant1Descriptor = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 1,
            TenantId = "tenant1",
            Kind = ActivityKind.Action
        };

        var tenant2Descriptor = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 5, // Much higher version but wrong tenant
            TenantId = "tenant2",
            Kind = ActivityKind.Action
        };

        var tenantAgnosticDescriptor = new ActivityDescriptor
        {
            TypeName = "TestActivity",
            Version = 2,
            TenantId = null,
            Kind = ActivityKind.Action
        };

        _registry.Register(tenant1Descriptor);
        _registry.Register(tenant2Descriptor);
        _registry.Register(tenantAgnosticDescriptor);

        // Act
        var result = _registry.Find("TestActivity");

        // Assert - should return tenant1 descriptor (not tenant2, even though it has higher version)
        Assert.NotNull(result);
        Assert.Equal("tenant1", result.TenantId);
        Assert.Equal(1, result.Version);
    }
}
