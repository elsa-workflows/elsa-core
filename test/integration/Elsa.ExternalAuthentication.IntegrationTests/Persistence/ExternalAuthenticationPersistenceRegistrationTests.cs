using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Services;
using Elsa.Persistence.EFCore.Modules.ExternalAuthentication;
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
    public void SqliteIdentityShellFeatureUsesEntityFrameworkCoreForExternalAuthentication()
    {
        var services = new ServiceCollection();
        services.AddExternalAuthenticationServices();

        var feature = new SqliteIdentityPersistenceShellFeature
        {
            ConnectionString = "Data Source=:memory:"
        };
        feature.ConfigureServices(services);

        var registration = services.Last(x => x.ServiceType == typeof(IIdentityProviderConnectionStore));

        Assert.Equal(typeof(EFCoreIdentityProviderConnectionStore), registration.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }
}
