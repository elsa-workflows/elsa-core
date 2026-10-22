using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Security;

/// <summary>Contributes the independently built Security Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Security", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class SecurityFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("security", "Security", "Elsa.ReactStudio.Security", false,
            typeof(SecurityFeature).Assembly, ["Elsa.Identity"]));
    }
}
