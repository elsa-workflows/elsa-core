using Elsa.Permissions;
using Elsa.Authorization;
using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.OpenIdConnect.Services;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Providers;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Identity.Contracts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationServiceCollectionTests
{
    [Test]
    public async Task AddsTheConfigurationFirstBrokerFoundation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISystemClock>(new TestSystemClock(new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero)));
        services.AddExternalAuthenticationServices(options =>
        {
            options.AllowedUnlinkedIdentityPolicyTypes.Clear();
            options.AllowedPermissionGrantSourceTypes.Clear();
        });

        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await Assert.That(serviceProvider.GetRequiredService<IOptions<ExternalAuthenticationOptions>>().Value).IsNotNull();
        var connectionSources = serviceProvider.GetRequiredService<IEnumerable<IIdentityProviderConnectionSource>>().ToArray();
        await Assert.That(connectionSources).Contains(source => source is ConfigurationIdentityProviderConnectionSource);
        await Assert.That(connectionSources).Contains(source => source is DatabaseIdentityProviderConnectionSource);
        await Assert.That(serviceProvider.GetRequiredService<IIdentityProviderConnectionRegistry>()).IsOfType(typeof(DefaultIdentityProviderConnectionRegistry));
        await Assert.That(serviceProvider.GetRequiredService<IExternalAuthenticationStateStore>()).IsOfType(typeof(InMemoryExternalAuthenticationStateStore));
        await Assert.That(serviceProvider.GetRequiredService<IAuthorizationGrantStore>()).IsOfType(typeof(InMemoryAuthorizationGrantStore));
        await Assert.That(serviceProvider.GetRequiredService<IExternalAuthenticationSessionStore>()).IsOfType(typeof(InMemoryExternalAuthenticationSessionStore));
        await Assert.That(serviceProvider.GetRequiredService<IPreviewResultStore>()).IsOfType(typeof(InMemoryPreviewResultStore));
        await Assert.That(serviceProvider.GetRequiredService<IConnectionObservationStore>()).IsOfType(typeof(InMemoryConnectionObservationStore));
        await Assert.That(serviceProvider.GetRequiredService<IConnectionRegistryVersionStore>()).IsOfType(typeof(InMemoryConnectionRegistryVersionStore));
        var descriptors = serviceProvider.GetServices<IPermissionDescriptorProvider>().SelectMany(x => x.GetDescriptors()).ToArray();
        await Assert.That(descriptors).Contains(x => x.Resource == ExternalAuthenticationResourcePermissions.Connections && x.Supports(CoreVerbs.View));
        await Assert.That(descriptors).Contains(x => x.Resource == ExternalAuthenticationResourcePermissions.PolicyDefaultRoles && x.Supports(CoreVerbs.Update));
        await Assert.That(serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>().Value).IsNotNull();
        await Assert.That(serviceProvider.GetServices<IConfigureOptions<RateLimiterOptions>>()).Contains(x => x.GetType().Name == "ConfigureExternalAuthenticationRateLimiterOptions");
    }

    [Test]
    public async Task OpenIdConnectRegistrationUsesTheHardenedProviderClient()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthenticationServices(options =>
        {
            options.AllowedUnlinkedIdentityPolicyTypes.Clear();
            options.AllowedPermissionGrantSourceTypes.Clear();
        });
        services.AddOpenIdConnectExternalAuthentication();

        await Assert.That(services).Contains(descriptor => descriptor.ServiceType == typeof(IProviderHttpClient) && descriptor.ImplementationFactory is not null);
        await Assert.That(services).Contains(descriptor => descriptor.ServiceType == typeof(OpenIdConnectExternalAuthenticationAdapter));
    }

    [Test]
    public async Task RoleDeletionContributorResolvesWhenIdentityIsNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISystemClock>(new TestSystemClock(DateTimeOffset.UnixEpoch));
        services.AddExternalAuthenticationServices(options =>
        {
            options.AllowedUnlinkedIdentityPolicyTypes.Clear();
            options.AllowedPermissionGrantSourceTypes.Clear();
        });

        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = serviceProvider.CreateScope();

        var contributor = await Assert.That(scope.ServiceProvider.GetServices<IRoleDeletionDependencyContributor>()).HasSingleItem();
        await Assert.That(contributor).IsOfType(typeof(ExternalAuthenticationRoleDeletionDependencyContributor));
        await Assert.That(scope.ServiceProvider.GetServices<IRoleStore>()).IsEmpty();
    }
}
