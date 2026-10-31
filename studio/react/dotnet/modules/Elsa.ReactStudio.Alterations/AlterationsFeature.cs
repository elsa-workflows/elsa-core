using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Alterations;

/// <summary>Contributes the independently built Alterations Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Alterations", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class AlterationsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("alterations", "Alterations", "Elsa.ReactStudio.Alterations", false,
            typeof(AlterationsFeature).Assembly, ["Elsa.Alterations"]));
    }
}
