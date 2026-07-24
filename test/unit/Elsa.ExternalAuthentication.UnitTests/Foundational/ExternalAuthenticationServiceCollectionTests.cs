using Elsa.Common;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.OpenIdConnect.Services;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Providers;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationServiceCollectionTests
{
    [Fact]
    public void AddsTheConfigurationFirstBrokerFoundation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISystemClock>(new TestSystemClock(new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero)));
        services.AddExternalAuthenticationServices(options =>
        {
            options.AllowedUnlinkedIdentityPolicyTypes.Clear();
            options.AllowedPermissionGrantSourceTypes.Clear();
        });

        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        Assert.NotNull(serviceProvider.GetRequiredService<IOptions<ExternalAuthenticationOptions>>().Value);
        var connectionSources = serviceProvider.GetRequiredService<IEnumerable<IIdentityProviderConnectionSource>>().ToArray();
        Assert.Contains(connectionSources, source => source is ConfigurationIdentityProviderConnectionSource);
        Assert.Contains(connectionSources, source => source is DatabaseIdentityProviderConnectionSource);
        Assert.IsType<DefaultIdentityProviderConnectionRegistry>(serviceProvider.GetRequiredService<IIdentityProviderConnectionRegistry>());
        Assert.IsType<InMemoryExternalAuthenticationStateStore>(serviceProvider.GetRequiredService<IExternalAuthenticationStateStore>());
        Assert.IsType<InMemoryAuthorizationGrantStore>(serviceProvider.GetRequiredService<IAuthorizationGrantStore>());
        Assert.IsType<InMemoryExternalAuthenticationSessionStore>(serviceProvider.GetRequiredService<IExternalAuthenticationSessionStore>());
        Assert.IsType<InMemoryPreviewResultStore>(serviceProvider.GetRequiredService<IPreviewResultStore>());
        Assert.IsType<InMemoryConnectionObservationStore>(serviceProvider.GetRequiredService<IConnectionObservationStore>());
        Assert.IsType<InMemoryConnectionRegistryVersionStore>(serviceProvider.GetRequiredService<IConnectionRegistryVersionStore>());
        Assert.Contains(serviceProvider.GetServices<IPermissionDescriptorProvider>().SelectMany(x => x.GetDescriptors()), x => x.Name == ExternalAuthenticationPermissions.ConnectionsRead);
        Assert.NotNull(serviceProvider.GetRequiredService<IOptions<RateLimiterOptions>>().Value);
        Assert.Contains(serviceProvider.GetServices<IConfigureOptions<RateLimiterOptions>>(), x => x.GetType().Name == "ConfigureExternalAuthenticationRateLimiterOptions");
    }

    [Fact]
    public void OpenIdConnectRegistrationUsesTheHardenedProviderClient()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthenticationServices(options =>
        {
            options.AllowedUnlinkedIdentityPolicyTypes.Clear();
            options.AllowedPermissionGrantSourceTypes.Clear();
        });
        services.AddOpenIdConnectExternalAuthentication();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IProviderHttpClient) && descriptor.ImplementationFactory is not null);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(OpenIdConnectExternalAuthenticationAdapter));
    }
}
