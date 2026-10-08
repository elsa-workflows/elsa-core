using Elsa.Authorization;
using Elsa.ModularServer.Web;
using Elsa.Permissions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Hosts.SmokeTests;

public class RoleManagementE2EFixtureTests
{
    [Fact]
    public void DisabledConfigurationLeavesServicesUnchanged()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ExistingService>();
        var before = services.ToArray();

        services.AddRoleManagementE2EFixtures(new ConfigurationBuilder().Build());

        Assert.Equal(before, services);
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(IPermissionDescriptorProvider));
    }

    [Fact]
    public void EnabledConfigurationContributesOneUnverifiedDescriptor()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [RoleManagementE2EFixtureServiceCollectionExtensions.IncludeUnverifiedPermissionDescriptorKey] = "true"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddRoleManagementE2EFixtures(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var providers = serviceProvider.GetServices<IPermissionDescriptorProvider>().ToArray();
        var descriptors = providers.SelectMany(x => x.GetDescriptors()).ToArray();

        Assert.Single(providers);
        var descriptor = Assert.Single(descriptors);
        Assert.Equal("e2e/role-management/unverified", descriptor.Resource);
        Assert.False(descriptor.Verified);
        Assert.Equal([CoreVerbs.View], descriptor.SupportedVerbs);
    }

    private sealed class ExistingService;
}
