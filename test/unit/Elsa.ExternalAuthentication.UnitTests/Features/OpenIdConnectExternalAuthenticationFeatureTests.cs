using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.OpenIdConnect.Features;
using Elsa.ExternalAuthentication.OpenIdConnect.Services;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenIdConnectExternalAuthenticationShellFeature = Elsa.ExternalAuthentication.OpenIdConnect.ShellFeatures.OpenIdConnectExternalAuthenticationFeature;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Features;

public class OpenIdConnectExternalAuthenticationFeatureTests
{
    [Test]
    public async Task ClassicFeatureRegistersTheOpenIdConnectAdapter()
    {
        var services = new ServiceCollection();
        var module = Substitute.For<IModule>();
        module.Services.Returns(services);
        var feature = new OpenIdConnectExternalAuthenticationFeature(module);

        feature.Apply();

        await AssertOpenIdConnectAdapterRegistered(services);
    }

    [Test]
    public async Task ShellFeatureRegistersTheOpenIdConnectAdapter()
    {
        var services = new ServiceCollection();
        var feature = new OpenIdConnectExternalAuthenticationShellFeature();

        feature.ConfigureServices(services);

        await AssertOpenIdConnectAdapterRegistered(services);
    }

    private static async Task AssertOpenIdConnectAdapterRegistered(IServiceCollection services)
    {
        await Assert.That(services).Contains(descriptor =>
            descriptor.ServiceType == typeof(IExternalAuthenticationAdapter) &&
            descriptor.ImplementationType == typeof(OpenIdConnectExternalAuthenticationAdapter));
        await Assert.That(services).Contains(descriptor =>
            descriptor.ServiceType == typeof(IAdapterSettingsMigration) &&
            descriptor.ImplementationType == typeof(OpenIdConnectSettingsV1Migration));
    }
}
