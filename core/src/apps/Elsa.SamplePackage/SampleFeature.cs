using CShells.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.SamplePackage;

public class SampleFeature(ILogger<SampleFeature> logger) : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        logger.LogInformation("Configuring services for SampleFeature");
        services.AddScoped<ISampleService, SampleService>();
    }
}