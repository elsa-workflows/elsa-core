# Elsa Dashboard API

`Elsa.Dashboard.Api` exposes aggregate endpoints used by Elsa Studio's operational dashboard. Hosts opt in by enabling the dashboard API feature/module; older hosts that do not install this module simply do not expose the `/dashboard/*` routes.

## Endpoints

### `GET /dashboard/overview`

Query parameters:

- `range`: Optional dashboard range key. Supported values are `1h`, `24h`, and `7d`. Unknown or missing values resolve to `24h`.
- `includeSystem`: Optional boolean. Defaults to `false`; when false, workflow instance aggregates exclude system workflows.

Returns:

- Backend and environment names.
- Runtime status, including whether the runtime is accepting work, active execution cycle count, ingress source count, and failed ingress source count.
- Contributor-composed workflow instance metrics for running, completed, faulted, suspended, interrupted, incident-bearing, and average completed duration.
- Contributor-composed structured log and console log diagnostic summaries.
- Applied range and resolved `from`/`to` timestamps.

### `POST /dashboard/workflow-trends`

Body:

- `range`: Optional range key.
- `granularity`: Optional bucket granularity. Defaults to `minute` for `1h`, `hour` for `24h`, and `day` for `7d`.
- `includeSystem`: Optional boolean.

Returns ordered buckets with created/started, finished, faulted, suspended, and incident-bearing counts.

### `GET /dashboard/needs-attention`

Query parameters:

- `range`: Optional range key.
- `take`: Optional maximum number of findings. Clamped to `1..50`.
- `includeSystem`: Optional boolean.

Returns priority-ordered findings derived from runtime state, workflow metrics, and available diagnostics summaries. Consumers should preserve backend ordering.

### `GET /dashboard/recent-activity`

Query parameters:

- `range`: Optional range key.
- `take`: Optional maximum number of workflow instance summaries. Clamped to `1..100`.
- `includeSystem`: Optional boolean.

Returns compact workflow instance activity ordered by latest update. The response intentionally omits workflow variables, inputs, outputs, and execution state.

### `POST /dashboard/workflow-hotspots`

Body:

- `range`: Optional range key.
- `metric`: One of `Faults`, `Executions`, `Incidents`, or `Duration`.
- `take`: Optional maximum number of rows. Clamped to `1..50`.
- `includeSystem`: Optional boolean.

Returns top workflow definitions for the selected metric. Studio treats this panel as optional and may omit it if the endpoint is unavailable.

## Capability States

Diagnostics summaries carry a `capability` object:

- `Available`: The diagnostic provider is installed and returned data.
- `NotInstalled`: The diagnostic provider is absent from the host.
- `Unauthorized`: The provider rejected access.
- `Unavailable`: The provider is installed but failed to produce a summary.

Dashboard overview degrades each diagnostics capability independently so a structured log failure does not prevent workflow metrics, runtime status, or console diagnostics from rendering.

## Extension Model

Dashboard core owns the public `/dashboard/*` routes, permissions, range resolution, and contributor orchestration. Feature modules own their own dashboard data. This keeps the dependency direction open for extension:

- `Elsa.Dashboard.Api` references `Elsa.Dashboard.Abstractions`.
- Feature modules reference `Elsa.Dashboard.Abstractions` and register one or more `IDashboardContributor` implementations.
- Dashboard core does not reference workflow, diagnostics, or future feature modules.

`IDashboardContributor` is intentionally broad enough for a module to contribute only the surfaces it owns:

- `GetOverviewAsync` for metric cards, panel summaries, runtime status, workflow metrics, and diagnostic summary slices.
- `GetFindingsAsync` for priority-ordered findings.
- `GetWorkflowTrendsAsync` for trend buckets.
- `GetRecentActivityAsync` for compact activity rows.
- `GetWorkflowHotspotsAsync` for hotspot rows.

Contributor failures are isolated by the dashboard composer. A failed contributor does not break the whole dashboard response; request cancellation is still honored.

### Backend Weather Example

```csharp
using Elsa.Dashboard.Abstractions.Contracts;
using Elsa.Dashboard.Abstractions.Extensions;
using Elsa.Dashboard.Abstractions.Models;

public class WeatherDashboardContributor(IWeatherService weatherService) : IDashboardContributor
{
    public string Id => "weather";
    public int Order => 500;

    public async ValueTask<DashboardOverviewContribution?> GetOverviewAsync(DashboardContext context)
    {
        var forecast = await weatherService.GetForecastAsync(context.CancellationToken);

        return new()
        {
            Panels =
            [
                new()
                {
                    Id = "weather.current",
                    Title = "Weather",
                    Summary = forecast.Summary,
                    Order = 10,
                    Navigation = new() { Kind = "Weather", Target = "current" }
                }
            ]
        };
    }
}

services.AddDashboardContributor<WeatherDashboardContributor>();
```

The example belongs in a hypothetical `Elsa.Weather.Dashboard` module or inside an existing Weather module, not in `Elsa.Dashboard.Api`.

### Studio Widget Model

Studio follows the same dependency direction. `Elsa.Studio.Dashboard` owns the dashboard route, refresh/range state, zone rendering, shared `DashboardWidgetContext`, and registration helpers. Feature modules register widgets into zones such as metrics, findings, primary panels, secondary panels, and diagnostics/status.

Minimal Studio Weather widget registration:

```csharp
services.AddDashboardWidget<WeatherDashboardWidget>(
    "weather.current",
    DashboardWidgetZones.SecondaryPanels,
    order: 500,
    title: "Weather",
    payloadKind: "Weather");
```

`WeatherDashboardWidget` can read the shared dashboard snapshot and navigation services from `DashboardWidgetContext`. The widget should live in `Elsa.Studio.Weather.Dashboard` or the Studio Weather module, not in `Elsa.Studio.Dashboard`.

### Diagnostics Migration

Structured-log and console-log dashboard behavior is diagnostics-owned:

- Backend summaries and findings are contributed by the corresponding diagnostics modules.
- Studio widgets are registered by the corresponding diagnostics Studio modules.
- Installing Dashboard alone does not install diagnostics. Installing diagnostics plus Dashboard causes diagnostics summaries and widgets to appear.

## Studio Integration Notes

- Studio should detect dashboard API support with a guarded dashboard call or feature metadata and show an explicit unavailable state when the endpoints are missing.
- Studio should keep the last successful dashboard snapshot visible after a refresh failure.
- Metric and finding targets should route to existing workflow instance, structured log, and console pages. Destination pages that do not yet support URL filters should still be linked and can add deep-link filters later.
- The API is read-only. Dashboard consumers must not add workflow write actions to the first dashboard slice.
