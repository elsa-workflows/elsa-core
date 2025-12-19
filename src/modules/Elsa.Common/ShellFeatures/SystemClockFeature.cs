using CShells.Features;
using Elsa.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.ShellFeatures;

/// <summary>
/// Configures the system clock.
/// </summary>
public class SystemClockFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
    }
}