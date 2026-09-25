using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.UI.Dashboard;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard;

[RemoteFeature(RemoteFeatureName)]
public class Feature(IDashboardWidgetRegistry widgetRegistry) : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Diagnostics.ConsoleLogs.Dashboard.ShellFeatures.ConsoleLogsDashboard";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        widgetRegistry.Add(new("diagnostics.console-logs", DashboardWidgetZones.DiagnosticsStatus, 200, typeof(ConsoleLogsDashboardWidget), "Console logs", RequiredBackendCapability: "ConsoleLogs", PayloadKind: "Diagnostics.ConsoleLogs"));

        return base.InitializeAsync(cancellationToken);
    }
}
