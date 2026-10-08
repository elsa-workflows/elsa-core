using Elsa.Studio.Authentication.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ServerOpenIdConnect = Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions.ServiceCollectionExtensions;
using WasmOpenIdConnect = Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions.ServiceCollectionExtensions;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

public class StudioAuthenticationProviderRegistrationTests
{
    [Fact]
    public void AddStudioAuthenticationProviderRegistration_DoesNotThrowOnFirstCall()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            services.AddStudioAuthenticationProviderRegistration(StudioAuthenticationProvider.OpenIdConnect));

        Assert.Null(exception);
        using var provider = services.BuildServiceProvider();
        Assert.Equal(
            StudioAuthenticationProvider.OpenIdConnect,
            Assert.Single(provider.GetServices<StudioAuthenticationProviderRegistration>()).Provider);
    }

    [Fact]
    public void AddOpenIdConnectAuth_Server_DoesNotThrowOnRegistration()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            ServerOpenIdConnect.AddOpenIdConnectAuth(services, options =>
            {
                options.Authority = "https://login.microsoftonline.com/{tenant}/v2.0";
                options.ClientId = "any-client-id";
            }));

        Assert.Null(exception);
        using var provider = services.BuildServiceProvider();
        Assert.Contains(
            provider.GetServices<StudioAuthenticationProviderRegistration>(),
            registration => registration.Provider == StudioAuthenticationProvider.OpenIdConnect);
    }

    [Fact]
    public void AddOpenIdConnectAuth_Wasm_DoesNotThrowOnRegistration()
    {
        var services = new ServiceCollection();

        var exception = Record.Exception(() =>
            WasmOpenIdConnect.AddOpenIdConnectAuth(services, options =>
            {
                options.Authority = "https://login.microsoftonline.com/{tenant}/v2.0";
                options.ClientId = "any-client-id";
            }));

        Assert.Null(exception);
        using var provider = services.BuildServiceProvider();
        Assert.Contains(
            provider.GetServices<StudioAuthenticationProviderRegistration>(),
            registration => registration.Provider == StudioAuthenticationProvider.OpenIdConnect);
    }
}
