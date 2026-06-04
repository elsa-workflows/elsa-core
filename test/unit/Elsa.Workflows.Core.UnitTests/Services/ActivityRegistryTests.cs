using Elsa.Common.Multitenancy;
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

    private readonly IActivityDescriber _activityDescriber;
    private readonly ILogger<ActivityRegistry> _logger;
    private readonly ActivityRegistry _registry;

    public ActivityRegistryTests()
    {
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        _activityDescriber = Substitute.For<IActivityDescriber>();
        _logger = Substitute.For<ILogger<ActivityRegistry>>();
        _registry = new(_activityDescriber, [], tenantAccessor, _logger);

        // Set default tenant for all tests
        tenantAccessor.TenantId.Returns(CurrentTenant);
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
    public void Find_ReturnsNextLatestVersion_WhenLatestDescriptorRemoved()
    {
        // Arrange
        var v1 = CreateDescriptor(TestActivityType, 1, CurrentTenant);
        var v2 = CreateDescriptor(TestActivityType, 2, CurrentTenant);
        var v3 = CreateDescriptor(TestActivityType, 3, CurrentTenant);
        RegisterDescriptors(v1, v2, v3);

        // Act
        _registry.Remove(typeof(ActivityRegistry), v3);
        var result = _registry.Find(TestActivityType);

        // Assert
        AssertDescriptor(result, CurrentTenant, 2);
    }

    [Fact]
    public void Find_KeepsLatestVersion_WhenNonLatestDescriptorRemoved()
    {
        // Arrange
        var v1 = CreateDescriptor(TestActivityType, 1, CurrentTenant);
        var v2 = CreateDescriptor(TestActivityType, 2, CurrentTenant);
        var v3 = CreateDescriptor(TestActivityType, 3, CurrentTenant);
        RegisterDescriptors(v1, v2, v3);

        // Act
        _registry.Remove(typeof(ActivityRegistry), v1);
        var result = _registry.Find(TestActivityType);

        // Assert
        AssertDescriptor(result, CurrentTenant, 3);
    }

    [Fact]
    public void Find_ReturnsNull_WhenProviderWithLatestDescriptorClearedAndNoDescriptorsRemain()
    {
        // Arrange
        var descriptor = CreateDescriptor(TestActivityType, 1, CurrentTenant);
        _registry.Add(typeof(Provider1), descriptor);

        // Act
        _registry.ClearProvider(typeof(Provider1));
        var result = _registry.Find(TestActivityType);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Find_RecomputesLatestVersion_WhenProviderWithLatestDescriptorCleared()
    {
        // Arrange
        var provider1V1 = CreateDescriptor(TestActivityType, 1, CurrentTenant);
        var provider1V3 = CreateDescriptor(TestActivityType, 3, CurrentTenant);
        var provider2V2 = CreateDescriptor(TestActivityType, 2, CurrentTenant);
        _registry.Add(typeof(Provider1), provider1V1);
        _registry.Add(typeof(Provider1), provider1V3);
        _registry.Add(typeof(Provider2), provider2V2);

        // Act
        _registry.ClearProvider(typeof(Provider1));
        var result = _registry.Find(TestActivityType);

        // Assert
        AssertDescriptor(result, CurrentTenant, 2);
    }

    [Fact]
    public void Find_ReturnsNull_WhenRegistryCleared()
    {
        // Arrange
        RegisterDescriptors(CreateDescriptor(TestActivityType, 2, CurrentTenant));

        // Act
        _registry.Clear();
        var result = _registry.Find(TestActivityType);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetDescriptorsAsync_ReturnsEmpty_WhenRegistryCleared()
    {
        // Arrange
        _activityDescriber.DescribeActivityAsync(typeof(ActivityRegistryTests), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(CreateDescriptor(TestActivityType, 1, CurrentTenant)));

        await _registry.RegisterAsync(typeof(ActivityRegistryTests), CancellationToken.None);

        // Act
        _registry.Clear();
        var descriptors = await _registry.GetDescriptorsAsync();

        // Assert
        Assert.Empty(descriptors);
        Assert.Empty(_registry.ListAll());
        Assert.Null(_registry.Find(TestActivityType));
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
            .Returns(new ValueTask<IEnumerable<ActivityDescriptor>>([descriptor1, descriptor2]));

        var providers = new[] { mockProvider };

        // Act - First refresh
        await _registry.RefreshDescriptorsAsync(providers);

        // Act - Second refresh (simulates the intentional repopulation in DefaultRegistriesPopulator)
        await _registry.RefreshDescriptorsAsync(providers);

        // Assert - Verify no warning logs were made
        _logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("was already registered")),
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
            .Returns(new ValueTask<IEnumerable<ActivityDescriptor>>([providerDescriptor]));

        var providers = new[] { mockProvider };

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
            Arg.Is<object>(v => v.ToString()!.Contains("ManualActivity")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task RefreshDescriptorsAsync_LogsWarning_WhenDifferentProvidersRegisterSameActivity()
    {
        // Arrange
        // Create two different providers with the same activity
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

        var provider1 = new Provider1([descriptor1]);
        var provider2 = new Provider2([descriptor2]);
        var providers = new IActivityProvider[] { provider1, provider2 };

        // Act
        await _registry.RefreshDescriptorsAsync(providers);

        // Assert - Should log a warning for the duplicate
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("DuplicateActivity") && v.ToString()!.Contains("was already registered")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task RefreshDescriptorsAsync_RecomputesLatestDescriptor_WhenProviderDropsLatestVersion()
    {
        // Arrange
        var provider = new MutableProvider(
        [
            CreateDescriptor(TestActivityType, 1, CurrentTenant),
            CreateDescriptor(TestActivityType, 3, CurrentTenant)
        ]);

        await _registry.RefreshDescriptorsAsync(provider);

        provider.Descriptors =
        [
            CreateDescriptor(TestActivityType, 1, CurrentTenant)
        ];

        // Act
        await _registry.RefreshDescriptorsAsync(provider);
        var result = _registry.Find(TestActivityType);

        // Assert
        AssertDescriptor(result, CurrentTenant, 1);
    }

    [Fact]
    public async Task RefreshDescriptorsAsync_PreservesExistingDescriptors_WhenProviderReturnsNoTenantGroups()
    {
        // Arrange
        var provider = new MutableProvider(
        [
            CreateDescriptor(TestActivityType, 2, CurrentTenant)
        ]);

        await _registry.RefreshDescriptorsAsync(provider);

        provider.Descriptors = [];

        // Act
        await _registry.RefreshDescriptorsAsync(provider);
        var result = _registry.Find(TestActivityType);

        // Assert
        AssertDescriptor(result, CurrentTenant, 2);
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
    private sealed class Provider1(IEnumerable<ActivityDescriptor> descriptors) : IActivityProvider
    {
        public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default) => new(descriptors);
    }

    private sealed class Provider2(IEnumerable<ActivityDescriptor> descriptors) : IActivityProvider
    {
        public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default) => new(descriptors);
    }

    private sealed class MutableProvider(IEnumerable<ActivityDescriptor> descriptors) : IActivityProvider
    {
        public IEnumerable<ActivityDescriptor> Descriptors { get; set; } = descriptors;

        public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default) => new(Descriptors);
    }

}
