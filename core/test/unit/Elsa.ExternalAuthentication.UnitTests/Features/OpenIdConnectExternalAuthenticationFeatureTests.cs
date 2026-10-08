using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.OpenIdConnect.Features;
using Elsa.ExternalAuthentication.OpenIdConnect.Services;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenIdConnectExternalAuthenticationShellFeature = Elsa.ExternalAuthentication.OpenIdConnect.ShellFeatures.OpenIdConnectExternalAuthenticationFeature;

namespace Elsa.ExternalAuthentication.UnitTests.Features;

public class OpenIdConnectExternalAuthenticationFeatureTests
{
    [Fact]
    public void ClassicFeatureRegistersTheOpenIdConnectAdapter()
    {
        var services = new ServiceCollection();
        var module = Substitute.For<IModule>();
        module.Services.Returns(services);
        var feature = new OpenIdConnectExternalAuthenticationFeature(module);

        feature.Apply();

        AssertOpenIdConnectAdapterRegistered(services);
    }

    [Fact]
    public void ShellFeatureRegistersTheOpenIdConnectAdapter()
    {
        var services = new ServiceCollection();
        var feature = new OpenIdConnectExternalAuthenticationShellFeature();

        feature.ConfigureServices(services);

        AssertOpenIdConnectAdapterRegistered(services);
    }

    private static void AssertOpenIdConnectAdapterRegistered(IServiceCollection services)
    {
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IExternalAuthenticationAdapter) &&
            descriptor.ImplementationType == typeof(OpenIdConnectExternalAuthenticationAdapter));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAdapterSettingsMigration) &&
            descriptor.ImplementationType == typeof(OpenIdConnectSettingsV1Migration));
    }
}
