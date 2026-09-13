using Elsa.Shells.Api.ShellFeatures;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace Elsa.Shells.Api.Tests;

public sealed class ShellsApiTestEntryPoint;

public sealed class ShellsApiWebApplicationFactory : TestWebApplicationFactory<ShellsApiTestEntryPoint>
{
    protected override IHostBuilder CreateHostBuilder() => new HostBuilder();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddFastEndpoints(options =>
                {
                    options.Assemblies = [typeof(ShellsApiFeature).Assembly];
                    options.DisableAutoDiscovery = true;
                });
                services.AddLogging();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapFastEndpoints());
            });
    }
}
