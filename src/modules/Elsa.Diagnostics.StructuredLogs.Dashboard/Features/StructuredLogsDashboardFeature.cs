using Elsa.Diagnostics.StructuredLogs.Dashboard.Extensions;
using Elsa.Diagnostics.StructuredLogs.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;

namespace Elsa.Diagnostics.StructuredLogs.Dashboard.Features;

[DependsOn(typeof(StructuredLogsFeature))]
public class StructuredLogsDashboardFeature(IModule module) : FeatureBase(module)
{
    public override void Apply()
    {
        Services.AddStructuredLogsDashboard();
    }
}
