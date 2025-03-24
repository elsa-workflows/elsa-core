using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.OpenTelemetry.Features;

public class OpenTelemetryFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
    }
}