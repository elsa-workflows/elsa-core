using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.UI.Dashboard;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Dashboard;

[RemoteFeature(RemoteFeatureName)]
public class Feature(IDashboardWidgetRegistry widgetRegistry) : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Diagnostics.StructuredLogs.Dashboard.ShellFeatures.StructuredLogsDashboard";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        widgetRegistry.Add(new("diagnostics.structured-logs", DashboardWidgetZones.DiagnosticsStatus, 100, typeof(StructuredLogsDashboardWidget), "Structured logs", RequiredBackendCapability: "StructuredLogs", PayloadKind: "Diagnostics.StructuredLogs"));

        return base.InitializeAsync(cancellationToken);
    }
}
