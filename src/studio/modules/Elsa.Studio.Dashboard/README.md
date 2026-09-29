# Elsa.Studio.Dashboard

`Elsa.Studio.Dashboard` provides the dashboard shell and shared widget composition contracts. It does not own workflow, console log, structured log, or OpenTelemetry data loading. Companion Studio modules register dashboard widgets only when their matching backend feature is enabled.

## Widget Composition

Dashboard widgets are contributed through `DashboardWidgetDescriptor`:

- `Id`: stable unique ID, for example `dashboard.workflow.trend`
- `Zone`: semantic placement, not a concrete grid coordinate
- `Order`: deterministic ordering within the zone
- `ComponentType`: Blazor component rendered by the shell
- `Title`: optional display name for tooling and diagnostics
- `RequiredBackendCapability`: backend capability needed by the widget
- `PayloadKind`: optional snapshot payload key used by the widget

Supported zones are:

- `DashboardWidgetZones.Metrics`: top-level KPI bands and compact counters
- `DashboardWidgetZones.Trend`: full-width execution charts and time-series panels
- `DashboardWidgetZones.Activity`: primary activity tables in the 8/12 operational column
- `DashboardWidgetZones.Findings`: prioritized status and attention panels
- `DashboardWidgetZones.SecondaryPanels`: supporting panels in the 4/12 operational column
- `DashboardWidgetZones.DiagnosticsStatus`: diagnostics status cards in the equal-width diagnostics row

`PrimaryPanels` is retained as a legacy zone and is rendered with the trend row so independently deployed companion modules remain visible. New widgets should use `Trend`, `Activity`, `Findings`, `SecondaryPanels`, or `DiagnosticsStatus` according to their operational role.

## Remote-Gated Registration

Register widgets from a companion feature that is marked with `RemoteFeatureAttribute`. The default feature service initializes that feature only when the backend advertises the matching shell feature.

```csharp
[RemoteFeature(RemoteFeatureName)]
public class ExampleDashboardFeature(IServiceProvider serviceProvider) : FeatureBase
{
    public const string RemoteFeatureName = "Example.Backend.ShellFeatures.ExampleDashboard";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        serviceProvider.GetService<IDashboardWidgetRegistry>()?.Add(new(
            "example.status",
            DashboardWidgetZones.DiagnosticsStatus,
            10,
            typeof(ExampleStatusDashboardWidget),
            "Example status",
            RequiredBackendCapability: "Example",
            PayloadKind: "Example.Status"));

        return ValueTask.CompletedTask;
    }
}
```

The optional registry lookup keeps companion modules usable when a host does not install `Elsa.Studio.Dashboard`.

## Widget Data Loading

The shell supplies a `DashboardWidgetContext` parameter with the selected range, load status, latest dashboard snapshot, refresh callback, and navigation manager. Widgets should read the snapshot payloads they need and keep their own empty and unavailable states.

Use `PayloadKind` to document which payload a widget consumes. For example, workflow dashboard widgets consume `WorkflowInstances`, `WorkflowTrends`, `RecentActivity`, and `WorkflowHotspots`; log widgets consume their matching dashboard snapshot payloads; and the OpenTelemetry companion owns loading `OpenTelemetry.StorageDiagnostics`.

## Host Registration

Install the dashboard shell and the companion modules you want:

```csharp
services.AddDashboardModule(backendApiConfig);
services.AddWorkflowsModule(backendApiConfig);
services.AddWorkflowsDashboardModule();
services.AddConsoleLogsModule(backendApiConfig);
services.AddConsoleLogsDashboardModule();
services.AddStructuredLogsModule(backendApiConfig);
services.AddStructuredLogsDashboardModule();
services.AddOpenTelemetryDiagnosticsModule(backendApiConfig);
services.AddOpenTelemetryDashboardModule();
```

The shell remains coherent when any companion is absent or when the backend does not advertise a companion dashboard feature.
