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
    [Fact]
    public void PersistenceRegistrationSuppliesTheDefaultHandleHasher()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthenticationEntityFrameworkCore();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType<HmacExternalAuthenticationHandleHasher>(
            serviceProvider.GetRequiredService<IExternalAuthenticationHandleHasher>());
    }

    [Fact]
    public void SqliteIdentityShellFeatureDoesNotRegisterExternalAuthenticationPersistence()
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

        Assert.Equal(typeof(InMemoryIdentityProviderConnectionStore), registration.ImplementationType);
    }

    [Fact]
    public void SqliteExternalAuthenticationShellFeatureRegistersEntityFrameworkCoreStores()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthenticationServices();

        var feature = new SqliteExternalAuthenticationPersistenceShellFeature
        {
            ConnectionString = "Data Source=:memory:"
        };
        feature.ConfigureServices(services);

        var connectionStore = services.Last(x => x.ServiceType == typeof(IIdentityProviderConnectionStore));
        Assert.Equal(typeof(EFCoreIdentityProviderConnectionStore), connectionStore.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, connectionStore.Lifetime);

        var sessionStore = services.Last(x => x.ServiceType == typeof(IExternalAuthenticationSessionStore));
        Assert.Equal(typeof(EFCoreExternalAuthenticationSessionStore), sessionStore.ImplementationType);
    }

    [Fact]
    public void StoresConsumedByTheSingletonConnectionSourceResolveFromTheRootProvider()
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

        Assert.IsType<EFCoreIdentityProviderConnectionStore>(serviceProvider.GetRequiredService<IIdentityProviderConnectionStore>());
        Assert.IsType<EFCoreConnectionRegistryVersionStore>(serviceProvider.GetRequiredService<IConnectionRegistryVersionStore>());
    }
}
