using System.Reflection;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using Elsa.Identity.Constants;
using Elsa.Identity.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace Elsa.ExternalAuthentication.IntegrationTests.Compatibility;

public sealed class LegacyIdentityEndpointWebApplicationFactory : TestWebApplicationFactory<ExternalAuthenticationTestEntryPoint>
{
    protected override IHostBuilder CreateHostBuilder() => new HostBuilder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        ExternalAuthenticationTestHost.Configure(
            builder,
            typeof(IdentityFeature).Assembly,
            endpoint => endpoint.Namespace is "Elsa.Identity.Endpoints.Login" or "Elsa.Identity.Endpoints.RefreshToken",
            IdentityAuthenticationSchemes.RefreshToken);
    }
}
