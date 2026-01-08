using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class ActivityRegistryTests
{
    [Fact]
    public async Task RefreshDescriptorsAsync_CalledTwice_DoesNotLogWarnings()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ActivityRegistry>>();
        var mockActivityDescriber = new Mock<IActivityDescriber>();
        var modifiers = Array.Empty<IActivityDescriptorModifier>();
        
        var registry = new ActivityRegistry(mockActivityDescriber.Object, modifiers, mockLogger.Object);
        
        var mockProvider = new Mock<IActivityProvider>();
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
        
        mockProvider.Setup(p => p.GetDescriptorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { descriptor1, descriptor2 });
        
        var providers = new[] { mockProvider.Object };
        
        // Act - First refresh
        await registry.RefreshDescriptorsAsync(providers);
        
        // Act - Second refresh (simulates the intentional repopulation in DefaultRegistriesPopulator)
        await registry.RefreshDescriptorsAsync(providers);
        
        // Assert - Verify no warning logs were made
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("was already registered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "Expected no duplicate registration warnings during refresh");
        
        // Verify descriptors are still registered
        var allDescriptors = registry.ListAll().ToList();
        Assert.Equal(2, allDescriptors.Count);
        Assert.Contains(allDescriptors, d => d.TypeName == "TestActivity1");
        Assert.Contains(allDescriptors, d => d.TypeName == "TestActivity2");
    }
    
    [Fact]
    public async Task RefreshDescriptorsAsync_PreservesManualDescriptors()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ActivityRegistry>>();
        var mockActivityDescriber = new Mock<IActivityDescriber>();
        var modifiers = Array.Empty<IActivityDescriptorModifier>();
        
        var registry = new ActivityRegistry(mockActivityDescriber.Object, modifiers, mockLogger.Object);
        
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
        
        registry.Register(manualDescriptor);
        
        // Create a provider descriptor
        var mockProvider = new Mock<IActivityProvider>();
        var providerDescriptor = new ActivityDescriptor
        {
            TypeName = "ProviderActivity",
            Version = 1,
            Kind = ActivityKind.Task,
            Category = "Provider",
            Description = "Provider activity",
            IsBrowsable = true
        };
        
        mockProvider.Setup(p => p.GetDescriptorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { providerDescriptor });
        
        var providers = new[] { mockProvider.Object };
        
        // Act - Refresh with provider
        await registry.RefreshDescriptorsAsync(providers);
        
        // Assert - Both manual and provider descriptors should be present
        var allDescriptors = registry.ListAll().ToList();
        Assert.Equal(2, allDescriptors.Count);
        Assert.Contains(allDescriptors, d => d.TypeName == "ManualActivity");
        Assert.Contains(allDescriptors, d => d.TypeName == "ProviderActivity");
        
        // Verify no warnings about manual descriptor being replaced
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ManualActivity")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never,
            "Expected no warnings about manual descriptor");
    }
    
    [Fact]
    public async Task RefreshDescriptorsAsync_LogsWarning_WhenDifferentProvidersRegisterSameActivity()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ActivityRegistry>>();
        var mockActivityDescriber = new Mock<IActivityDescriber>();
        var modifiers = Array.Empty<IActivityDescriptorModifier>();
        
        var registry = new ActivityRegistry(mockActivityDescriber.Object, modifiers, mockLogger.Object);
        
        // Create two different providers with the same activity
        var mockProvider1 = new Mock<IActivityProvider>();
        var mockProvider2 = new Mock<IActivityProvider>();
        
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
        
        mockProvider1.Setup(p => p.GetDescriptorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { descriptor1 });
        
        mockProvider2.Setup(p => p.GetDescriptorsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { descriptor2 });
        
        var providers = new[] { mockProvider1.Object, mockProvider2.Object };
        
        // Act
        await registry.RefreshDescriptorsAsync(providers);
        
        // Assert - Should log a warning for the duplicate
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DuplicateActivity") && v.ToString()!.Contains("was already registered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once,
            "Expected one warning for duplicate activity from different providers");
    }
}
