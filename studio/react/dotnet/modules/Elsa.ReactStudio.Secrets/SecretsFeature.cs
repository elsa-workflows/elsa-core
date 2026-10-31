using CShells.Features;
using Elsa.ReactStudio.Host;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Secrets;

/// <summary>Contributes the independently built Secrets Preview module when enabled.</summary>
[ShellFeature("ReactStudio.Secrets", DependsOn = [typeof(ReactStudioPreviewFeature)])]
public sealed class SecretsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ReactStudioModule("secrets", "Secrets", "Elsa.ReactStudio.Secrets", false,
            typeof(SecretsFeature).Assembly, ["Elsa.Secrets"]));
    }
}
