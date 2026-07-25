using Elsa.Features.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ModuleIdentityFeature = Elsa.Identity.Features.IdentityFeature;
using ShellIdentityFeature = Elsa.Identity.ShellFeatures.IdentityFeature;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultAccessTokenIssuerRegistrationTests
{
    [Fact]
    public void ModuleFeatureResolvesThePreferredAccessTokenIssuerConstructor()
    {
        var services = CreateServices();
        var module = Substitute.For<IModule>();
        module.Services.Returns(services);
        new ModuleIdentityFeature(module).Apply();

        AssertAccessTokenIssuerResolves(services);
    }

    [Fact]
    public void ShellFeatureResolvesThePreferredAccessTokenIssuerConstructor()
    {
        var services = CreateServices();
        new ShellIdentityFeature().ConfigureServices(services);

        AssertAccessTokenIssuerResolves(services);
    }

    private static ServiceCollection CreateServices()
    {
        return new ServiceCollection();
    }

    private static void AssertAccessTokenIssuerResolves(IServiceCollection services)
    {
        services.AddScoped(_ => Substitute.For<IElsaTokenService>());
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        Assert.IsType<DefaultAccessTokenIssuer>(scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>());
    }
}
