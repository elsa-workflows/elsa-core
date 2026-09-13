using System.Reflection;
using Elsa.ExternalAuthentication.IntegrationTests.Fixtures;
using Elsa.Identity.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace Elsa.ExternalAuthentication.IntegrationTests.Identity;

public sealed class RoleDeletionEndpointContractWebApplicationFactory : TestWebApplicationFactory<ExternalAuthenticationTestEntryPoint>
{
    protected override IHostBuilder CreateHostBuilder() => new HostBuilder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        ExternalAuthenticationTestHost.Configure(builder, typeof(IdentityFeature).Assembly, endpoint => endpoint.Namespace == "Elsa.Identity.Endpoints.Roles.Delete");
    }
}
