using CShells.Features;
using Elsa.ReactStudio.Spike;
using Microsoft.Extensions.DependencyInjection;

namespace Example.ReactStudio.Spike.Weather;

[ShellFeature("Example.Weather", DependsOn = [typeof(PreviewFeature)])]
public sealed class WeatherFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services) => services.AddSingleton(
        new StudioModule("weather", "0.0.1-spike", 0, 0, typeof(WeatherFeature).Assembly));
}

[ShellFeature("Example.Incompatible", DependsOn = [typeof(PreviewFeature)])]
public sealed class IncompatibleFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new StudioModule("incompatible-host", "0.0.1-spike", 1, 0, typeof(IncompatibleFeature).Assembly));
        services.AddSingleton(new StudioModule("incompatible-sdk", "0.0.1-spike", 0, 1, typeof(IncompatibleFeature).Assembly));
    }
}
