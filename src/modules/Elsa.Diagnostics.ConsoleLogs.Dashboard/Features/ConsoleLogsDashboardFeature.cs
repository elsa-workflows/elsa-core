using Elsa.Diagnostics.ConsoleLogs.Dashboard.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;

namespace Elsa.Diagnostics.ConsoleLogs.Dashboard.Features;

[DependsOn(typeof(ConsoleLogsFeature))]
public class ConsoleLogsDashboardFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.AddConsoleLogsDashboard();
    }
}
