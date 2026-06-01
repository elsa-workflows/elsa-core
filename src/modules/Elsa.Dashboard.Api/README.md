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
- Workflow instance metrics for running, completed, faulted, suspended, interrupted, incident-bearing, and average completed duration.
- Structured log and console log diagnostic summaries.
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

## Studio Integration Notes

- Studio should detect dashboard API support with a guarded dashboard call or feature metadata and show an explicit unavailable state when the endpoints are missing.
- Studio should keep the last successful dashboard snapshot visible after a refresh failure.
- Metric and finding targets should route to existing workflow instance, structured log, and console pages. Destination pages that do not yet support URL filters should still be linked and can add deep-link filters later.
- The API is read-only. Dashboard consumers must not add workflow write actions to the first dashboard slice.
