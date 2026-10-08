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

    [Fact]
    public void ModuleFeatureRegistersUserDeletionCoordinator()
    {
        var services = CreateServices();
        var module = Substitute.For<IModule>();
        module.Services.Returns(services);
        new ModuleIdentityFeature(module).Apply();

        AssertUserDeletionCoordinatorResolves(services);
    }

    [Fact]
    public void ShellFeatureRegistersUserDeletionCoordinator()
    {
        var services = CreateServices();
        new ShellIdentityFeature().ConfigureServices(services);

        AssertUserDeletionCoordinatorResolves(services);
    }

    [Fact]
    public void ModuleFeatureRegistersInMemorySessionRevocation()
    {
        var services = CreateServices();
        var module = Substitute.For<IModule>();
        module.Services.Returns(services);
        new ModuleIdentityFeature(module).Apply();

        AssertInMemorySessionRevocationResolves(services);
    }

    [Fact]
    public void ShellFeatureRegistersInMemorySessionRevocation()
    {
        var services = CreateServices();
        new ShellIdentityFeature().ConfigureServices(services);

        AssertInMemorySessionRevocationResolves(services);
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

    // SessionRevoker is checked by registration: resolving it binds the token options, which the shell feature reads from shell configuration.
    private static void AssertInMemorySessionRevocationResolves(IServiceCollection services)
    {
        Assert.Contains(services, x => x.ServiceType == typeof(SessionRevoker));
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        Assert.IsType<MemoryRevokedSessionStore>(scope.ServiceProvider.GetRequiredService<IRevokedSessionStore>());
    }

    private static void AssertUserDeletionCoordinatorResolves(IServiceCollection services)
    {
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        Assert.IsType<UserDeletionCoordinator>(scope.ServiceProvider.GetRequiredService<IUserDeletionCoordinator>());
    }
}
