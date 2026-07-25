using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Services;
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
}
