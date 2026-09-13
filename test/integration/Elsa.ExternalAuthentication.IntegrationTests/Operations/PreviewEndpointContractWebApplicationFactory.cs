using System.Reflection;
using Elsa.ExternalAuthentication.Features;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace Elsa.ExternalAuthentication.IntegrationTests.Operations;

public sealed class PreviewEndpointContractWebApplicationFactory : TestWebApplicationFactory<ExternalAuthenticationTestEntryPoint>
{
    protected override IHostBuilder CreateHostBuilder() => new HostBuilder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        ExternalAuthenticationTestHost.Configure(builder, typeof(ExternalAuthenticationFeature).Assembly, endpoint => endpoint.Namespace == "Elsa.ExternalAuthentication.Endpoints.Previews", addRateLimiter: true);
    }
}
