using CShells.AspNetCore.Features;
using CShells.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.ReactStudio.Host;

/// <summary>Opt-in server feature that exposes the Preview shell and enabled module manifests.</summary>
[ShellFeature("ReactStudioPreview")]
public sealed class ReactStudioPreviewFeature(IConfiguration configuration) : IWebShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
        endpoints.MapReactStudio(options =>
        {
            options.Path = configuration["ReactStudio:Path"] ?? "/studio-preview";
            options.DefaultServerUrl = configuration["ReactStudio:ServerUrl"];
            options.ConfigurationFile = configuration["ReactStudio:ConfigurationFile"] ??
                System.IO.Path.Combine(AppContext.BaseDirectory, "studio.config.json");
        });
    }
}
