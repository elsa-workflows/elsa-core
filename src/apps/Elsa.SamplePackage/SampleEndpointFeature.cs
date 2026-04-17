using CShells.AspNetCore.Features;
using CShells.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.SamplePackage;

[ShellFeature(DependsOn = [typeof(SampleFeature)])]
public class SampleEndpointFeature(ILogger<SampleFeature> logger) : IWebShellFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
        endpoints.MapGet("/sample-endpoint", async (HttpContext context, ISampleService sampleService) =>
        {
            logger.LogInformation("Sample endpoint was hit.");
            sampleService.DoSomething();
            await context.Response.WriteAsync("Hello from the sample endpoint!");
        });
    }

    public void ConfigureServices(IServiceCollection services)
    {
    }
}