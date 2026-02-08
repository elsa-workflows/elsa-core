using Elsa.Common.Multitenancy;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Workflows.Core.UnitTests.Services;

/// <summary>
/// Unit tests for ActivityRegistry, specifically testing multi-tenant descriptor resolution logic and refresh behavior.
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

    [Fact]
    public async Task RefreshDescriptorsAsync_CalledTwice_DoesNotLogWarnings()
    {
        // Arrange
        var modifiers = Array.Empty<IActivityDescriptorModifier>();
        
        var mockProvider = Substitute.For<IActivityProvider>();
        var descriptor1 = new ActivityDescriptor
        {
            TypeName = "TestActivity1",
            Version = 1,
            Kind = ActivityKind.Task,
            Category = "Test",
            Description = "Test Activity 1",
            IsBrowsable = true
        };
        
        var descriptor2 = new ActivityDescriptor
        {
            TypeName = "TestActivity2",
            Version = 1,
            Kind = ActivityKind.Task,
            Category = "Test",
            Description = "Test Activity 2",
            IsBrowsable = true
        };
        
        mockProvider.GetDescriptorsAsync(Arg.Any<CancellationToken>())
            .ReturnsAsync(new[] { descriptor1, descriptor2 });
        
        var providers = new[] { mockProvider.Object };
        
        // Act - First refresh
        await _registry.RefreshDescriptorsAsync(providers);
        
        // Act - Second refresh (simulates the intentional repopulation in DefaultRegistriesPopulator)
        await _registry.RefreshDescriptorsAsync(providers);
        
        // Assert - Verify no warning logs were made
        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>((v, t) => v.ToString()!.Contains("was already registered")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
        
        // Verify descriptors are still registered
        var allDescriptors = _registry.ListAll().ToList();
        Assert.Equal(2, allDescriptors.Count);
        Assert.Contains(allDescriptors, d => d.TypeName == "TestActivity1");
        Assert.Contains(allDescriptors, d => d.TypeName == "TestActivity2");
    }
    
    [Fact]
    public async Task RefreshDescriptorsAsync_PreservesManualDescriptors()
    {
        // Arrange
        var modifiers = Array.Empty<IActivityDescriptorModifier>();
        
        // Create a manual descriptor
        var manualDescriptor = new ActivityDescriptor
        {
            TypeName = "ManualActivity",
            Version = 1,
            Kind = ActivityKind.Task,
            Category = "Manual",
            Description = "Manually registered activity",
            IsBrowsable = true
        };
        
        _registry.Register(manualDescriptor);
        
        // Create a provider descriptor
        var mockProvider = Substitute.For<IActivityProvider>();
        var providerDescriptor = new ActivityDescriptor
        {
            TypeName = "ProviderActivity",
            Version = 1,
            Kind = ActivityKind.Task,
            Category = "Provider",
            Description = "Provider activity",
            IsBrowsable = true
        };
        
        mockProvider.GetDescriptorsAsync(Arg.Any<CancellationToken>())
            .ReturnsAsync(new[] { providerDescriptor });
        
        var providers = new[] { mockProvider.Object };
        
        // Act - Refresh with provider
        await _registry.RefreshDescriptorsAsync(providers);
        
        // Assert - Both manual and provider descriptors should be present
        var allDescriptors = _registry.ListAll().ToList();
        Assert.Equal(2, allDescriptors.Count);
        Assert.Contains(allDescriptors, d => d.TypeName == "ManualActivity");
        Assert.Contains(allDescriptors, d => d.TypeName == "ProviderActivity");
        
        // Verify no warnings about manual descriptor being replaced
        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>((v, t) => v.ToString()!.Contains("ManualActivity")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
    
    [Fact]
    public async Task RefreshDescriptorsAsync_LogsWarning_WhenDifferentProvidersRegisterSameActivity()
    {
        // Arrange
        var modifiers = Array.Empty<IActivityDescriptorModifier>();
        
        // Create two different providers with the same activity
        var mockProvider1 = Substitute.For<IActivityProvider>();
        var mockProvider2 = Substitute.For<IActivityProvider>();
        
        var descriptor1 = new ActivityDescriptor
        {
            TypeName = "DuplicateActivity",
            Version = 1,
            Kind = ActivityKind.Task,
            Category = "Test",
            Description = "From Provider 1",
            IsBrowsable = true
        };
        
        var descriptor2 = new ActivityDescriptor
        {
            TypeName = "DuplicateActivity",
            Version = 1,
            Kind = ActivityKind.Task,
            Category = "Test",
            Description = "From Provider 2",
            IsBrowsable = true
        };
        
        mockProvider1.GetDescriptorsAsync(Arg.Any<CancellationToken>())
            .ReturnsAsync(new[] { descriptor1 });
        
        mockProvider2.GetDescriptorsAsync(Arg.Any<CancellationToken>())
            .ReturnsAsync(new[] { descriptor2 });
        
        var providers = new[] { mockProvider1.Object, mockProvider2.Object };
        
        // Act
        await _registry.RefreshDescriptorsAsync(providers);
        
        // Assert - Should log a warning for the duplicate
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>((v, t) => v.ToString()!.Contains("DuplicateActivity") && v.ToString()!.Contains("was already registered")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}