using Elsa.Common.Contracts;
using Elsa.Common.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Features;

/// <summary>
/// Configures the system clock.
/// </summary>
public class SystemClockFeature : FeatureBase
{
    /// <inheritdoc />
    public SystemClockFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<ISystemClock, SystemClock>();
    }
}