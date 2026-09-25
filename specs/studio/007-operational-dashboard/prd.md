# PRD: Elsa Studio Operational Dashboard

## Summary

Replace the current simple Elsa Studio dashboard with an operational MudBlazor dashboard host that helps users understand backend health, recent workflow activity, active issues, and diagnostics status at a glance. The dashboard should be dense, practical, linked to existing Studio pages, and extensible: installed Studio modules should be able to contribute their own dashboard widgets without the dashboard module hard-coding module-specific panels.

The first screen should answer:

- Is the selected Elsa backend connected and accepting work?
- How many workflow instances are running, completed, faulted, suspended, or incident-bearing?
- What changed over the selected time range?
- What needs attention right now?
- Which recent workflow instances should I inspect?
- Are structured logs and console logs healthy when those modules are installed?

## Problem

The current dashboard page is a static welcome surface with a documentation link. It does not help operators, administrators, or developers understand the state of the backend. Studio already has rich workflow, structured log, and console pages, but users need a first-stop overview that summarizes those areas and routes them to the right drill-down.

Without a dashboard-shaped backend API, Studio would need to issue many small API calls and approximate aggregates client-side. The preferred solution is for Studio to consume a backend dashboard API module from Elsa Core, while keeping graceful fallback states for older backends.

## Goals

- Build a useful operational dashboard as the Studio home page.
- Provide dashboard widget infrastructure that other Studio modules can reference.
- Add a small `Elsa.Studio.Dashboard.Abstractions` package for widget contribution contracts.
- Use MudBlazor components and existing Studio layout/menu conventions.
- Consume dashboard-shaped backend endpoints when available.
- Show clear unavailable/unauthorized states when the backend does not expose dashboard data.
- Let contributed widgets link metric cards, findings, and tables to existing Workflow Instances, Structured Logs, Console, or future module pages.
- Keep the interface compact and work-focused rather than marketing-oriented.
- Support selected time ranges such as `1h`, `24h`, and `7d`.
- Refresh data manually and optionally at a conservative interval.

## Non-Goals

- Do not add workflow write actions directly to the dashboard in the first slice.
- Do not duplicate full workflow instance filtering or log viewing on the dashboard.
- Do not require the diagnostics modules to be installed.
- Do not hard-code diagnostics, console, workflow, or future module widgets inside the dashboard host.
- Do not build a custom charting library.
- Do not make the dashboard a landing page or documentation page.
- Do not display raw workflow variables, inputs, outputs, or raw console lines.

## Users

- Workflow operators checking production or staging health.
- Administrators triaging workflow incidents.
- Developers validating local backend behavior.
- Support engineers looking for a fast route to workflow and diagnostics pages.

## Backend Dependency

Preferred backend module: `Elsa.Dashboard.Api`

Preferred endpoints:

- `GET /dashboard/overview`
- `POST /dashboard/workflow-trends`
- `GET /dashboard/needs-attention`
- `GET /dashboard/recent-activity`
- `POST /dashboard/workflow-hotspots`

The dashboard host should detect the backend dashboard capability through installed features or a guarded API call and expose that state through the widget context. Individual widget providers remain responsible for their own module-specific capability and permission checks. If the dashboard API is unavailable, the host may show a limited fallback state, while module widgets may omit themselves or render an unavailable state.

## Existing Studio Context

Current dashboard module:

```text
src/modules/Elsa.Studio.Dashboard/
├── Feature.cs
├── Extensions/ServiceCollectionExtensions.cs
├── Menu/DashboardMenu.cs
└── Pages/Index.razor
```

The current page contains only:

- Page heading: Dashboard
- Text: Manage all the things
- Documentation alert

The replacement should keep the route `/` and the existing dashboard menu item.

## Architecture

### Package Split

Add a small abstractions package:

```text
src/modules/Elsa.Studio.Dashboard.Abstractions/
├── Contracts/
│   ├── IDashboardWidgetProvider.cs
│   └── IDashboardWidgetComponent.cs
├── Models/
│   ├── DashboardWidgetDescriptor.cs
│   ├── DashboardWidgetContext.cs
│   ├── DashboardWidgetPlacement.cs
│   ├── DashboardWidgetSize.cs
│   ├── DashboardWidgetAvailability.cs
│   └── DashboardWidgetRefreshMode.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

`Elsa.Studio.Dashboard.Abstractions` should be stable and small. It should not depend on `Elsa.Studio.Dashboard`, and it should not contain concrete widget UI. Other Studio modules reference this package to contribute widgets. The dashboard module references this package and acts as the host.

### Widget Contribution Contract

Suggested contract:

```csharp
public interface IDashboardWidgetProvider
{
    ValueTask<IEnumerable<DashboardWidgetDescriptor>> GetWidgetsAsync(
        DashboardWidgetContext context,
        CancellationToken cancellationToken = default);
}

public interface IDashboardWidgetComponent
{
}

public record DashboardWidgetDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required Type ComponentType { get; init; }
    public DashboardWidgetPlacement Placement { get; init; } = DashboardWidgetPlacement.Main;
    public DashboardWidgetSize Size { get; init; } = DashboardWidgetSize.Medium;
    public int Order { get; init; }
    public DashboardWidgetAvailability Availability { get; init; } = DashboardWidgetAvailability.Available;
    public string? UnavailableReason { get; init; }
    public DashboardWidgetRefreshMode RefreshMode { get; init; } = DashboardWidgetRefreshMode.Dashboard;
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = ImmutableDictionary<string, object?>.Empty;
}

public record DashboardWidgetContext
{
    public required string Range { get; init; }
    public bool IncludeSystemWorkflows { get; init; }
    public string? BackendEnvironmentName { get; init; }
    public IReadOnlyDictionary<string, object?> BackendMetadata { get; init; } = ImmutableDictionary<string, object?>.Empty;
    public IReadOnlyDictionary<string, bool> BackendCapabilities { get; init; } = ImmutableDictionary<string, bool>.Empty;
    public DateTimeOffset RefreshGeneratedAt { get; init; }
}
```

The contract uses `ImmutableDictionary` from `System.Collections.Immutable`; add the corresponding namespace and package reference to `Elsa.Studio.Dashboard.Abstractions` if it is not already available transitively.

`IDashboardWidgetComponent` is a marker contract for Razor components rendered by the host through `DynamicComponent`. Widget components should implement it and accept the following conventional `[Parameter]` properties:

- `DashboardWidgetContext Context`: current dashboard range, capability state, and refresh generation.
- `DashboardWidgetDescriptor Descriptor`: widget metadata supplied by the provider.
- Additional entries from `DashboardWidgetDescriptor.Parameters`, merged by the host into the `DynamicComponent` parameter dictionary.

The host should validate that `ComponentType` implements both Blazor `IComponent` and `IDashboardWidgetComponent` before rendering. `Context` and `Descriptor` are reserved parameter names; providers must not supply entries with those names. The host should reject duplicate reserved parameters, log the provider error, and render the widget error chrome instead of overriding silently.

Widget `Id` values must be globally unique across all providers and should use a module-qualified prefix, such as `workflows.metric.running` or `diagnostics.structured-logs.health`. If duplicate IDs appear across providers or within a single provider response, the host should keep the first descriptor in provider registration order and descriptor order, skip later duplicates, and log the duplicate provider and widget Id.

`Parameters` is immutable snapshot data in the descriptor contract. Providers should create a fresh immutable dictionary per descriptor, and the host should clone entries into a separate merged parameter dictionary when adding `Context` and `Descriptor`.

`DashboardWidgetSize` should define grid-span intent:

- `Small`: compact metric card or status chip.
- `Medium`: standard panel in `Main` or `Side`.
- `Large`: prominent chart, table, or findings panel.
- `Wide`: full-row content, normally paired with `FullWidth` placement.

`DashboardWidgetPlacement` should sort using explicit host precedence: `Summary`, `Main`, `Side`, `FullWidth`, then `Footer`. The host should not rely on enum numeric values for layout ordering.

`DashboardWidgetAvailability` should define host chrome behavior:

- `Available`: render the widget component.
- `Unavailable`: render consistent host unavailable chrome with the descriptor title and optional provider-supplied reason.
- `Unauthorized`: render consistent host unauthorized chrome without leaking unavailable data details. The host must ignore `UnavailableReason` for unauthorized widgets even if a provider supplies one.

`DashboardWidgetRefreshMode` should define host behavior:

- `Dashboard`: refresh on page load, user refresh, and range/filter changes.
- `Live`: refresh on page load, user refresh, range/filter changes, and any dashboard auto-refresh cadence when enabled.
- `Static`: do not refresh due to dashboard data changes; rediscover only when the module set, permissions, or navigation context changes.

`DashboardWidgetContext` should include:

- `Range`: selected dashboard range, such as `1h`, `24h`, or `7d`.
- `IncludeSystemWorkflows`: include-system-workflows flag, if the host exposes it.
- `BackendEnvironmentName`: current backend/environment name when available.
- `BackendMetadata`: additional backend metadata needed by contributed widgets without expanding the core contract.
- `BackendCapabilities`: backend dashboard capability state keyed by stable capability name.
- `RefreshGeneratedAt`: current refresh timestamp.

The method-level `CancellationToken` on `GetWidgetsAsync` is the single cancellation source for provider discovery. The context should not carry a second token.

The provider should return only widgets owned by its module. For example:

```csharp
services.AddScoped<IDashboardWidgetProvider, StructuredLogsDashboardWidgetProvider>();
services.AddScoped<IDashboardWidgetProvider, ConsoleLogsDashboardWidgetProvider>();
services.AddScoped<IDashboardWidgetProvider, WorkflowsDashboardWidgetProvider>();
```

### Dashboard Host Responsibilities

`Elsa.Studio.Dashboard` owns:

- Route `/` and existing dashboard menu item.
- Page header, range selector, refresh button, last refreshed timestamp, and backend status shell.
- Widget discovery from all registered `IDashboardWidgetProvider` services.
- Widget sorting, placement, layout, spacing, and responsive behavior. Sorting should be deterministic by explicit `DashboardWidgetPlacement` precedence, then `Order`, then widget `Id` using ordinal string comparison.
- Page-level loading, no-provider, provider-discovery error, unavailable, and unauthorized chrome; widget components own their internal loading, empty, and recoverable error states after `Availability=Available`.
- Rendering widgets through Blazor `DynamicComponent`.
- `DashboardWidgetSurface.razor` wraps each widget in a Blazor `ErrorBoundary` so a render exception in one contributed component shows `DashboardWidgetError.razor` widget-level error chrome and does not collapse the whole dashboard.
- Passing descriptor parameters and dashboard context to widgets.
- Stable layout regions and MudBlazor visual conventions.

The host must not know about Structured Logs, Console Logs, Workflows, Secrets, or future feature-specific widget internals.

### Contributing Module Responsibilities

Each contributing module owns:

- Its widget provider registration.
- Its widget component implementation.
- Its own backend client calls or use of shared dashboard API data.
- Feature installation, backend capability, and permission checks.
- Widget-specific empty/error states.
- Widget-specific navigation targets.

For example, `Elsa.Studio.Diagnostics.ConsoleLogs` contributes the Console Logs widget only when the Console Logs Studio module is installed and the paired backend feature/permission is available. `Elsa.Studio.Diagnostics.StructuredLogs` does the same for Structured Logs. A future module should be able to add a dashboard widget by referencing only `Elsa.Studio.Dashboard.Abstractions`.

## Information Architecture

### Header

Content:

- Page title: `Dashboard`
- Backend/environment indicator when available
- Runtime status chip:
  - `Accepting work`
  - `Paused`
  - `Draining`
  - `Unavailable`
- Last refreshed timestamp
- Refresh icon button
- Time range segmented control:
  - `1h`
  - `24h`
  - `7d`

MudBlazor components:

- `MudStack`
- `MudText`
- `MudChip`
- `MudToggleGroup`
- `MudToggleItem`
- `MudIconButton`
- `MudTooltip`

### Widget Regions

The host should expose layout regions rather than fixed panels:

- `Summary`: compact metric cards across the top.
- `Main`: large primary widgets such as charts and tables.
- `Side`: narrower widgets such as findings, diagnostics snapshots, or status summaries.
- `FullWidth`: wide widgets such as dense recent activity tables.
- `Footer`: lower-priority widgets.

These region names are the required `DashboardWidgetPlacement` enum values. The dashboard host maps `DashboardWidgetPlacement` and `DashboardWidgetSize` to MudBlazor grid spans. Widgets should not create nested page-level cards; the host supplies the outer widget surface.

### Workflow Summary Widgets

The workflow module should contribute metric widgets such as:

Cards:

- Running
- Completed in range
- Faulted in range
- Suspended
- Average duration
- Warnings / needs attention

Each card should include:

- Label
- Value
- Optional delta or range caption
- Icon
- Severity color only when meaningful
- Click target where a drill-down exists

MudBlazor components:

- `MudGrid`
- `MudItem`
- `MudPaper`
- `MudIcon`
- `MudText`

### Module-Scoped Needs Attention Widgets

Prioritized list of findings returned by the backend for the contributing module's scope. The dashboard host should not own a cross-module needs-attention widget, and the workflow module should not render diagnostics findings. Each contributing module should either call a feature-owned endpoint or pass a stable `scope` value to `GET /dashboard/needs-attention` so it receives only findings it owns.

First-party needs-attention scopes should be stable kebab-case values:

- `workflows`
- `structured-logs`
- `console-logs`

Workflow examples:

- `12 workflows faulted in the last 24 hours`
- `Runtime is paused`
- `1 ingress source failed to pause`

Structured Logs examples:

- `Structured log storage dropped writes`

Console Logs examples:

- `3 console log sources are stale`
- `stderr volume increased`

Behavior:

- Empty state: quiet success state, not a large illustration.
- Findings link to relevant pages.
- Severity shown with compact color and icon.
- Limited to a practical number, e.g. 8.

MudBlazor components:

- `MudList`
- `MudListItem`
- `MudIcon`
- `MudChip`
- `MudButton`

### Execution Trend Widget

Line or stacked chart showing activity over the selected time range.

Series:

- Created or started
- Finished
- Faulted
- Suspended or incident-bearing, if useful

MudBlazor components:

- `MudChart`
- `MudPaper`
- `MudSkeleton` while loading

### Recent Workflow Activity Widget

Compact table of recent workflow instances.

Columns:

- Workflow / instance
- Status
- Sub-status
- Incidents
- Duration
- Updated
- Action icon

Behavior:

- Click row opens the workflow instance viewer.
- Faulted/interrupted rows should be visually scannable.
- Use dense mode.
- Avoid full workflow state loading on the dashboard.

MudBlazor components:

- `MudTable`
- `MudChip`
- `MudIconButton`
- `MudTooltip`

### Diagnostics Widgets

Diagnostics widgets are contributed by diagnostics modules, not hard-coded by the dashboard host.

Structured Logs widget content:

- Structured log source count and stale count.
- Recent error/critical count.
- Structured log dropped event/write count.
- Link to Structured Logs page when available.

Console Logs widget content:

- Console log source count and stale count.
- Recent stderr count.
- Console dropped-line count.
- Link to Console page when available.

Behavior:

- If a diagnostics module is absent, its provider is not registered and the widget is not contributed.
- If the module is installed but its backend feature is unavailable, the provider may omit the widget or return it with `DashboardWidgetAvailability.Unavailable`.
- If unauthorized, the provider may omit the widget or return it with `DashboardWidgetAvailability.Unauthorized` so the host can show `No access`.
- Do not display raw log lines.

### Workflow Hotspots Widget

Optional lower-priority panel showing top workflows by selected metric:

- Faults
- Executions
- Incidents
- Duration

This panel is contributed by the workflow module and is part of the workflow dashboard widget contract, but should render only when the backend exposes the hotspot endpoint. If a backend does not support hotspots yet, the provider should omit this widget or mark it unavailable.

## Visual Design Direction

The dashboard should feel like an operational admin tool:

- Dense but not crowded.
- White or neutral background with outlined panels.
- 8px or smaller border radius, unless existing Studio theme dictates otherwise.
- Color used primarily for semantic severity.
- Avoid marketing hero treatment.
- Avoid decorative gradients and large empty cards.
- Keep cards at one level only; do not nest cards inside cards.
- Use icons in buttons and status surfaces.
- Use compact typography inside cards and tables.

## Component Plan

Suggested Studio additions. The abstractions package follows the package split defined in the Architecture section; the concrete host and contributing modules add:

```text
src/modules/Elsa.Studio.Dashboard/
├── Client/
│   └── IDashboardApi.cs
├── Models/
│   ├── DashboardOverview.cs
│   ├── DashboardRange.cs
│   ├── DashboardCapabilityStatus.cs
│   ├── DashboardWorkflowInstanceMetrics.cs
│   ├── DashboardRuntimeStatus.cs
│   ├── DashboardFinding.cs
│   ├── DashboardNeedsAttentionResponse.cs
│   ├── DashboardTrendRequest.cs
│   ├── DashboardTrendResponse.cs
│   ├── DashboardTrendBucket.cs
│   ├── DashboardRecentActivityItem.cs
│   ├── DashboardRecentActivityResponse.cs
│   ├── DashboardHotspot.cs
│   ├── DashboardWorkflowHotspotsRequest.cs
│   └── DashboardWorkflowHotspotsResponse.cs
├── Services/
│   ├── DashboardService.cs
│   ├── DashboardWidgetRegistry.cs
│   ├── DashboardWidgetLayoutService.cs
│   └── DashboardRangeMapper.cs
├── Components/
│   ├── DashboardWidgetHost.razor
│   ├── DashboardWidgetSurface.razor
│   ├── DashboardWidgetError.razor
│   ├── DashboardWidgetUnavailable.razor
│   ├── DashboardWidgetUnauthorized.razor
│   └── DashboardRuntimeChip.razor
└── Pages/
    ├── Index.razor
    ├── Index.razor.cs
    └── Index.razor.css

src/modules/Elsa.Studio.Workflows/
└── Dashboard/
    ├── WorkflowsDashboardWidgetProvider.cs
    └── Components/
        ├── WorkflowMetricWidget.razor
        ├── WorkflowNeedsAttention.razor
        ├── WorkflowTrendChart.razor
        ├── WorkflowRecentActivityTable.razor
        └── WorkflowHotspotsWidget.razor

src/modules/Elsa.Studio.Diagnostics.StructuredLogs/
└── Dashboard/
    ├── StructuredLogsDashboardWidgetProvider.cs
    └── Components/
        └── StructuredLogsDashboardWidget.razor

src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/
└── Dashboard/
    ├── ConsoleLogsDashboardWidgetProvider.cs
    └── Components/
        └── ConsoleLogsDashboardWidget.razor
```

Contributed widget components in this plan are content-only and render inside `DashboardWidgetSurface.razor`. Feature-specific navigation target mapping belongs to the contributing module that owns the widget descriptor, not the dashboard host.

Response and request model boundaries:

- `DashboardTrendRequest`: selected range, bucket granularity, and `includeSystem`.
- `DashboardTrendResponse`: ordered trend buckets plus the applied range and granularity.
- `DashboardNeedsAttentionResponse`: ordered findings plus source capability metadata.
- `DashboardRecentActivityResponse`: ordered recent workflow activity items plus the applied range.
- `DashboardWorkflowHotspotsRequest`: selected range, metric, take count, and `includeSystem`.
- `DashboardWorkflowHotspotsResponse`: ordered hotspot rows plus the applied range and metric.

## Client API Contract

Add a Refit-style Studio client:

```csharp
public interface IDashboardApi
{
    [Get("/dashboard/overview")]
    Task<DashboardOverview> GetOverviewAsync(
        string? range = null,
        bool includeSystem = false,
        CancellationToken cancellationToken = default);

    [Post("/dashboard/workflow-trends")]
    Task<DashboardTrendResponse> GetWorkflowTrendsAsync(
        [Body] DashboardTrendRequest request,
        CancellationToken cancellationToken = default);

    [Get("/dashboard/needs-attention")]
    Task<DashboardNeedsAttentionResponse> GetNeedsAttentionAsync(
        string? range = null,
        string? scope = null,
        int take = 8,
        bool includeSystem = false,
        CancellationToken cancellationToken = default);

    [Get("/dashboard/recent-activity")]
    Task<DashboardRecentActivityResponse> GetRecentActivityAsync(
        string? range = null,
        int take = 20,
        bool includeSystem = false,
        CancellationToken cancellationToken = default);

    [Post("/dashboard/workflow-hotspots")]
    Task<DashboardWorkflowHotspotsResponse> GetWorkflowHotspotsAsync(
        [Body] DashboardWorkflowHotspotsRequest request,
        CancellationToken cancellationToken = default);
}
```

## Page States

The dashboard must handle:

- Initial loading.
- Successful full data load.
- Partial capability data.
- Dashboard API unavailable.
- Unauthorized dashboard API.
- Backend disconnected.
- Empty workflow data.
- Refresh failure after previous successful data.
- No registered widget providers.
- A widget provider throwing during discovery.
- A widget component failing while other widgets remain renderable.

Do not replace the whole page with an error if stale data exists; show a compact error alert and keep the last successful snapshot.

## Navigation

Metric and finding click targets:

- Running workflows -> Workflow Instances filtered by `Status=Running`.
- Faulted workflows -> Workflow Instances filtered by `SubStatus=Faulted` or `HasIncidents=true`.
- Suspended workflows -> Workflow Instances filtered by `SubStatus=Suspended`.
- Interrupted workflow findings -> Workflow Instances filtered by `SubStatus=Interrupted`.
- Recent activity row -> Workflow Instance viewer.
- Structured log errors -> Structured Logs page with level/time filters.
- Console stderr -> Console page with stream/time filters.

If URL filter support is missing in the destination page, linking should still navigate to the relevant page and preserve a TODO for later filter deep links.

## Functional Requirements

- The dashboard route remains `/`.
- The dashboard menu label remains `Dashboard`.
- The dashboard host discovers widgets from all registered `IDashboardWidgetProvider` services.
- The dashboard host renders contributed widgets by placement, size, and order.
- The workflow module contributes overview, workflow-scoped needs-attention, trend, recent activity, and workflow hotspot widgets for the selected range.
- The Structured Logs module contributes structured log widgets, including structured-log-scoped needs-attention when supported, only when that Studio module is installed.
- The Console Logs module contributes console log widgets, including console-log-scoped needs-attention when supported, only when that Studio module is installed.
- The user can switch range between `1h`, `24h`, and `7d`.
- The user can refresh manually.
- The page shows last refreshed time.
- Metric cards render zero values explicitly.
- Needs-attention findings are scoped to the contributing module and ordered by backend priority.
- Recent activity table uses dense layout and links to instance details.
- The dashboard host renders consistent available, unavailable, and unauthorized surfaces from `DashboardWidgetAvailability`; widget components own only their internal empty and error states after `Availability=Available`.
- The page remains usable on laptop and mobile widths.

## Non-Functional Requirements

- First meaningful dashboard render should happen within 2 seconds against a local backend.
- The dashboard should avoid excessive polling; default to manual refresh unless product direction asks for auto-refresh.
- Use cancellation tokens for all API calls.
- Dispose any timers or cancellation resources.
- Avoid layout shift by reserving stable card/table dimensions.
- Do not use raw strings for route construction where Studio has existing route helpers.
- Keep components small and testable.

## Accessibility

- Icon-only buttons require tooltips and aria labels.
- Severity must not rely only on color.
- Tables need clear column labels.
- Loading skeletons must not obscure focus.
- Refresh and range controls must be keyboard usable.

## Testing

Unit tests:

- Widget provider discovery and deterministic ordering.
- Widget placement and size mapping to host layout regions.
- Range mapping to backend duration values.
- Capability state mapping.
- Module-owned navigation target mapping for contributed widgets.
- Metric formatting for zero, large values, durations, and unavailable values.
- Needs-attention severity/icon mapping.

Component/manual verification:

- Full data.
- Empty data.
- Dashboard API unavailable.
- Unauthorized.
- Partial widget availability.
- No diagnostics modules installed.
- Only one diagnostics module installed.
- A widget provider/component failure does not break the whole dashboard.
- Faulted/interrupted/suspended findings.
- Mobile and desktop layout.

## Rollout

Phase 1:

- Add `Elsa.Studio.Dashboard.Abstractions`.
- Add dashboard widget host infrastructure to `Elsa.Studio.Dashboard`.
- Add dashboard client/service/models shared by widgets that consume `Elsa.Dashboard.Api`.
- Replace static dashboard page with operational widget layout.
- Move workflow dashboard panels into workflow-contributed widgets.
- Add Structured Logs and Console Logs widget providers in their respective modules.
- Gracefully handle unavailable backend dashboard API.

Phase 2:

- Add deep link filters to destination pages where missing.
- Add optional auto-refresh setting.

Phase 3:

- Add tenant/environment-aware filters if backend supports them.
- Add user customization, if product demand appears.

## Acceptance Criteria

- The Studio home page is an operational dashboard, not a welcome/documentation page.
- The dashboard uses MudBlazor and existing Studio conventions.
- `Elsa.Studio.Dashboard.Abstractions` exists and can be referenced by widget-contributing modules without referencing the concrete dashboard module.
- `Elsa.Studio.Dashboard` discovers and renders registered widget providers.
- A dashboard-capable backend renders host-owned runtime state independently of workflow widgets.
- A dashboard-capable backend plus installed workflow module renders metrics, workflow-scoped needs-attention list, trend chart, recent activity, and workflow hotspots when supported.
- Installed diagnostics modules contribute their own widgets; absent diagnostics modules do not produce empty hard-coded panels.
- A backend without dashboard API renders a clear limited/unavailable state.
- Unauthorized dashboard data is handled without a broken page.
- Clicking metric cards or findings navigates to relevant Studio pages.
- The layout is coherent on desktop and mobile.
- Tests cover core mapping and formatting logic.

## Open Questions

- Should Studio include a limited fallback mode using existing workflow APIs before the backend dashboard API lands?
- Should dashboard data auto-refresh by default, or stay manual for predictable backend load?
- Should the dashboard expose an `include system workflows` toggle in the first version?
- Should runtime status be visible to every dashboard reader or only users with workflow instance read permission?
- Should a future version add user preferences for hiding unavailable widgets that providers elect to return?
