using CShells.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ReactStudio.Spike.Labels;

[ShellFeature("ReactStudioSpike.Labels", DependsOn = [typeof(PreviewFeature)])]
public sealed class LabelsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services) => services.AddSingleton(
        new StudioModule("labels", "0.0.1-spike", 0, 0, typeof(LabelsFeature).Assembly));
}
