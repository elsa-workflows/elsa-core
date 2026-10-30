using Elsa.Diagnostics.OpenTelemetry.Extensions;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Diagnostics.OpenTelemetry.Features;

public class OpenTelemetryFeature(IModule module) : FeatureBase(module)
{
    public Action<OpenTelemetryDiagnosticsOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<OpenTelemetryFeature>();
    }

    public override void Apply()
    {
        Services.AddOpenTelemetryDiagnosticsServices(ConfigureOptions);
        Module.AddFastEndpointsFromModule();
    }
}
