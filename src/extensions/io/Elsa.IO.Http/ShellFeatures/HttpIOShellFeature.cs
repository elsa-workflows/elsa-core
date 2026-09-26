using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.IO.Http.ShellFeatures;

/// <summary>
/// Shell feature for HTTP I/O operations.
/// </summary>
[ShellFeature(
    DisplayName = "HTTP I/O",
    Description = "Provides HTTP-based I/O activities for workflows",
    DependsOn = ["I/O"])]
[UsedImplicitly]
public class HttpIOShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
    }
}

