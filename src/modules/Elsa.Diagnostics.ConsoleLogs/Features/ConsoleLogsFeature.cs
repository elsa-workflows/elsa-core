using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Diagnostics.ConsoleLogs.Features;

public class ConsoleLogsFeature(IModule module) : FeatureBase(module)
{
    public Action<ConsoleLogsOptions>? ConfigureOptions { get; set; }

    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<ConsoleLogsFeature>();
    }

    public override void Apply()
    {
        Services.AddConsoleLogsServices(ConfigureOptions);
        Module.AddFastEndpointsFromModule();
    }
}
