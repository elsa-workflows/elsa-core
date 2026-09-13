using Elsa.Authorization;
using Elsa.ModularServer.Web;
using Elsa.Permissions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Hosts.SmokeTests;

public class RoleManagementE2EFixtureTests
{
    [Test]
    public async Task DisabledConfigurationLeavesServicesUnchanged()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ExistingService>();
        var before = services.ToArray();

        services.AddRoleManagementE2EFixtures(new ConfigurationBuilder().Build());

        await Assert.That(services)
            .IsEquivalentTo(before, TUnit.Assertions.Enums.CollectionOrdering.Matching)
            .Using((actual, expected) => ReferenceEquals(actual, expected));
        await Assert.That(services).DoesNotContain(x => x.ServiceType == typeof(IPermissionDescriptorProvider));
    }

    [Test]
    public async Task EnabledConfigurationContributesOneUnverifiedDescriptor()
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

        await Assert.That(providers).HasSingleItem();
        var descriptor = (await Assert.That(descriptors).HasSingleItem())!;
        await Assert.That(descriptor.Resource).IsEqualTo("e2e/role-management/unverified");
        await Assert.That(descriptor.Verified).IsFalse();
        await Assert.That(descriptor.SupportedVerbs).IsEquivalentTo([CoreVerbs.View], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    private sealed class ExistingService;
}
