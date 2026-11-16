using Elsa.Tenants.Options;
using Elsa.Tenants.ShellFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.UnitTests.ShellFeatures;

public class TenantsFeatureTests
{
    [Fact]
    public void ConfigureServices_EnablesTenancy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();

        // Act
        new TenantsFeature().ConfigureServices(services);

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        Assert.True(serviceProvider.GetRequiredService<IOptions<TenantsOptions>>().Value.IsEnabled);
    }
}
