using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Persistence.EFCore.Sqlite.ShellFeatures;
using Elsa.ExternalAuthentication.Persistence.EFCore.Stores;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.IntegrationTests.Persistence;

public class ExternalAuthenticationPersistenceRegistrationTests
{
    [Test]
    public async Task PersistenceRegistrationSuppliesTheDefaultHandleHasher()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthenticationEntityFrameworkCore();

        using var serviceProvider = services.BuildServiceProvider();

        await Assert.That(
            serviceProvider.GetRequiredService<IExternalAuthenticationHandleHasher>()).IsOfType(typeof(HmacExternalAuthenticationHandleHasher));
    }

    [Test]
    public async Task SqliteIdentityShellFeatureDoesNotRegisterExternalAuthenticationPersistence()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthenticationServices();

        var feature = new SqliteIdentityPersistenceShellFeature
        {
            ConnectionString = "Data Source=:memory:"
        };
        feature.ConfigureServices(services);

        // External authentication persistence has its own feature; enabling identity persistence must not imply it.
        var registration = services.Last(x => x.ServiceType == typeof(IIdentityProviderConnectionStore));

        await Assert.That(registration.ImplementationType).IsEqualTo(typeof(InMemoryIdentityProviderConnectionStore));
    }

    [Test]
    public async Task SqliteExternalAuthenticationShellFeatureRegistersEntityFrameworkCoreStores()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthenticationServices();

        var feature = new SqliteExternalAuthenticationPersistenceShellFeature
        {
            ConnectionString = "Data Source=:memory:"
        };
        feature.ConfigureServices(services);

        var connectionStore = services.Last(x => x.ServiceType == typeof(IIdentityProviderConnectionStore));
        await Assert.That(connectionStore.ImplementationType).IsEqualTo(typeof(EFCoreIdentityProviderConnectionStore));
        await Assert.That(connectionStore.Lifetime).IsEqualTo(ServiceLifetime.Singleton);

        var sessionStore = services.Last(x => x.ServiceType == typeof(IExternalAuthenticationSessionStore));
        await Assert.That(sessionStore.ImplementationType).IsEqualTo(typeof(EFCoreExternalAuthenticationSessionStore));
    }

    [Test]
    public async Task StoresConsumedByTheSingletonConnectionSourceResolveFromTheRootProvider()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthenticationServices();

        var feature = new SqliteExternalAuthenticationPersistenceShellFeature
        {
            ConnectionString = "Data Source=:memory:"
        };
        feature.ConfigureServices(services);

        // DatabaseIdentityProviderConnectionSource is registered as a singleton and takes both of these stores, so
        // neither may be scoped. Under ValidateScopes, resolving a scoped service from the root provider throws.
        // ValidateOnBuild is deliberately off: this container holds external authentication only, so unrelated
        // descriptors are legitimately unsatisfiable here.
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        await Assert.That(serviceProvider.GetRequiredService<IIdentityProviderConnectionStore>()).IsOfType(typeof(EFCoreIdentityProviderConnectionStore));
        await Assert.That(serviceProvider.GetRequiredService<IConnectionRegistryVersionStore>()).IsOfType(typeof(EFCoreConnectionRegistryVersionStore));
    }
}
