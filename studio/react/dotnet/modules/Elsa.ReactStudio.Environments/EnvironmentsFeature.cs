using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Environments;

/// <summary>Contributes the independently built Environments Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Environments", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class EnvironmentsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("environments", "Environments", "Elsa.ReactStudio.Environments", false,
            typeof(EnvironmentsFeature).Assembly, []));
    }
}
