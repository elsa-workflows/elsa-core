using Elsa.Tenants.Options;
using Elsa.Tenants.ShellFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.UnitTests.ShellFeatures;

public class TenantsFeatureRejectUnresolvedTenantsTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ConfigureServices_AppliesRejectUnresolvedTenants(bool featureSetting, bool expectedOption)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();

        // Act
        new TenantsFeature { RejectUnresolvedTenants = featureSetting }.ConfigureServices(services);

        // Assert
        using var serviceProvider = services.BuildServiceProvider();
        Assert.Equal(expectedOption, serviceProvider.GetRequiredService<IOptions<MultitenancyOptions>>().Value.RejectUnresolvedTenants);
    }
}
