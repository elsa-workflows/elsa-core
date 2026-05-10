using Elsa.Diagnostics.StructuredLogs.Extensions;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Diagnostics.StructuredLogs.Features;

public class StructuredLogsFeature(IModule module) : FeatureBase(module)
{
    public Action<StructuredLogsOptions>? ConfigureOptions { get; set; }
    
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<StructuredLogsFeature>();
    }
    
    public override void Apply()
    {
        Services.AddStructuredLogsServices(ConfigureOptions);
        Module.AddFastEndpointsFromModule();
    }
}
