using Elsa.Studio.Abstractions;
using Elsa.Studio.Attributes;
using Elsa.Studio.Authorization;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard.UI.Dashboard;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Dashboard;

[RemoteFeature(RemoteFeatureName)]
public class Feature(IDashboardWidgetRegistry widgetRegistry) : FeatureBase
{
    public const string RemoteFeatureName = "Elsa.Diagnostics.OpenTelemetry.ShellFeatures.OpenTelemetry";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        widgetRegistry.Add(new("diagnostics.open-telemetry", DashboardWidgetZones.DiagnosticsStatus, 300, typeof(OpenTelemetryDashboardWidget), "OpenTelemetry", RequiredBackendCapability: "OpenTelemetry", PayloadKind: "OpenTelemetry.StorageDiagnostics")
        {
            // The widget loads its figures from the OpenTelemetry API, which dashboard:view does not open.
            RequiredPermissions = [new(OpenTelemetryPermissions.OpenTelemetry, PermissionVerbs.View)]
        });

        return base.InitializeAsync(cancellationToken);
    }
}
