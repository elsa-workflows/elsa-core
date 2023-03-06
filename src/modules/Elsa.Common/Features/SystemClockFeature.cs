using Elsa.Common.Contracts;
using Elsa.Common.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Features;

public class SystemClockFeature : FeatureBase
{
    public SystemClockFeature(IModule module) : base(module)
    {
    }

    public override void Apply()
    {
        Services.AddSingleton<ISystemClock, SystemClock>();
    }
}