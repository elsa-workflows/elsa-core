using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Shell;

/// <summary>Contributes the independently built Shell Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Shell", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class ShellFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("shell", "Shell", "Elsa.ReactStudio.Shell", true,
            typeof(ShellFeature).Assembly, []));
    }
}
