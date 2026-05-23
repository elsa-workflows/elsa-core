# PRD: Operational Dashboard Backend API

## Summary

Add a backend dashboard API module to Elsa Core that exposes a small set of read-only operational aggregates for Elsa Studio and other clients. The module should make it cheap and consistent to answer whether an Elsa backend is healthy, what workflow activity is happening, what needs attention, and which workflow definitions are contributing most to recent load or faults.

The dashboard API should aggregate data from existing workflow management, runtime administration, package, feature, and diagnostics services. It should avoid requiring Studio to orchestrate many separate list requests, avoid client-side approximation where server-side aggregation is available, and keep provider-specific query optimizations behind backend service contracts.

## Problem

Elsa Studio currently has a simple dashboard page that does not expose operational value. A useful dashboard needs counts, trends, recent activity, health signals, and prioritized issues. Most raw data already exists in Elsa Core, but the current API surface is detail/list oriented:

- Workflow instance list APIs can return counts through `TotalCount`, but each metric requires a separate query.
- Workflow trends require multiple timestamp-bucketed queries from the client.
- Average duration, top failing workflows, and top active workflows require fetching and grouping rows client-side.
- Runtime status exists, but it is tied to the runtime admin permission.
- Diagnostics APIs expose recent buffers, sources, and storage diagnostics, but not dashboard-shaped summaries.

This leads to chatty clients, inconsistent calculations, weaker permissions, and poor room for backend optimizations.

## Goals

- Provide a single dashboard overview endpoint for first-paint dashboard data.
- Provide workflow trend and hotspot endpoints for charts and operational drill-down panels.
- Provide a prioritized needs-attention endpoint that combines workflow, runtime, and diagnostics signals.
- Keep the API read-only and safe for users who can view operations but cannot administer runtime state.
- Reuse existing Elsa stores and services for the initial implementation.
- Allow future persistence providers to optimize aggregations without changing Studio contracts.
- Work in single-node, multi-shell, and diagnostic-feature-optional deployments.

## Non-Goals

- Do not replace existing workflow instance, workflow definition, diagnostics, or runtime admin APIs.
- Do not introduce durable analytics storage in the first implementation.
- Do not require diagnostics modules to be installed for workflow dashboard data to work.
- Do not add write actions such as pause, resume, retry, cancel, or delete to the dashboard API.
- Do not expose raw log text through dashboard endpoints beyond short recent issue summaries that already respect diagnostics permissions.
- Do not implement a dedicated metrics database or OpenTelemetry backend in this slice.

## Users

- Operators monitoring a running Elsa backend.
- Workflow administrators triaging faulted or blocked workflow instances.
- Developers validating local and test environments.
- Studio users who have read access to workflow/runtime information but not necessarily runtime management permissions.

## Existing Backend Surface

The following endpoints can be reused or referenced, but should not be the primary dashboard integration path:

| Capability | Existing endpoint | Notes |
| --- | --- | --- |
| Workflow instance list and counts | `GET/POST /workflow-instances` | Supports status, sub-status, incidents, system workflow filtering, and timestamp filters on `CreatedAt`, `UpdatedAt`, `FinishedAt`. `TotalCount` can be used as a count. |
| Workflow definition count | `GET /workflow-definitions/query/count` | Counts logical workflow definitions. |
| Workflow definition list | `GET /workflow-definitions` | Useful for recent definitions and definition metadata. |
| Runtime status | `GET /admin/workflow-runtime/status` | Returns accepting-work state, pause/drain state, ingress sources, and active execution cycle count. Currently uses management permission. |
| Package version | `GET /package/version` | Anonymous version endpoint. |
| Installed features | `GET /features/installed`, `GET /features/installed/{fullName}` | Used for optional capability awareness. |
| Structured logs | `GET/POST /diagnostics/structured-logs/recent`, `GET /diagnostics/structured-logs/sources`, `GET /diagnostics/structured-logs/storage` | Optional module; gated by diagnostics permissions. |
| Console logs | `POST /diagnostics/console-logs/recent`, `GET /diagnostics/console-logs/sources` | Optional module; gated by diagnostics permissions. |

## Proposed Module

Add a new module:

```text
src/modules/Elsa.Dashboard.Api/
```

The module should register FastEndpoints and a read-only dashboard service layer. Suggested feature names:

- `DashboardApiFeature`
- shell feature: `DashboardApiFeature`

The module should depend on workflow management/runtime API infrastructure where needed, but optional diagnostics integrations should be discovered through service availability so the dashboard still works when diagnostics modules are not installed.

## Permissions

Add a read-only permission:

```text
read:dashboard
```

The dashboard API should also honor underlying data sensitivity:

- Workflow aggregates require `read:dashboard` and should not expose individual workflow data beyond dashboard-safe summaries.
- Workflow instance summary links should only be returned when the user can read workflow instances, or the endpoint should require both `read:dashboard` and `read:workflow-instances`.
- Runtime status summary should not require `ManageWorkflowRuntime`; it should expose read-only status through `read:dashboard`.
- Diagnostics summary fields should be included only when the diagnostics feature is installed and the caller has the matching diagnostics read permission, or they should return an unavailable/unauthorized capability marker.

## Endpoint 1: Dashboard Overview

```http
GET /dashboard/overview?range=PT24H&includeSystem=false
```

Returns first-paint dashboard data.

### Request

- `range`: ISO-8601 duration, default `PT24H`, allowed values or bounded custom durations up to 30 days.
- `includeSystem`: boolean, default `false`.
- Optional future filters:
  - `definitionId`
  - `tenantId` for explicitly authorized cross-tenant administration only
  - `environment`

### Response Shape

```json
{
  "generatedAt": "2026-05-23T12:00:00Z",
  "range": {
    "from": "2026-05-22T12:00:00Z",
    "to": "2026-05-23T12:00:00Z",
    "duration": "PT24H"
  },
  "package": {
    "version": "3.8.0"
  },
  "capabilities": {
    "workflowInstances": "available",
    "workflowDefinitions": "available",
    "runtimeStatus": "available",
    "structuredLogs": "available",
    "consoleLogs": "available"
  },
  "workflowDefinitions": {
    "total": 128
  },
  "workflowInstances": {
    "total": 12482,
    "statuses": {
      "running": 450,
      "finished": 12032
    },
    "subStatuses": {
      "pending": 118,
      "executing": 237,
      "suspended": 91,
      "interrupted": 4,
      "finished": 11976,
      "faulted": 37,
      "cancelled": 19
    },
    "withIncidents": 42,
    "completedInRange": 840,
    "faultedInRange": 12,
    "averageDurationMs": 842,
    "averageDurationAccuracy": {
      "accuracy": "Exact",
      "sampleSize": 840
    }
  },
  "runtime": {
    "isAcceptingNewWork": true,
    "reason": "None",
    "activeExecutionCycleCount": 3,
    "sources": [
      {
        "name": "InternalBookmarkQueue",
        "state": "Running",
        "lastError": null,
        "lastTransitionAt": "2026-05-23T11:59:00Z"
      }
    ]
  },
  "diagnostics": {
    "structuredLogs": {
      "sourceCount": 1,
      "staleSourceCount": 0,
      "errorCountInRange": 5,
      "criticalCountInRange": 0,
      "droppedEvents": 0,
      "droppedWrites": 0
    },
    "consoleLogs": {
      "sourceCount": 1,
      "staleSourceCount": 0,
      "stderrCountInRange": 9,
      "droppedLines": 0
    }
  }
}
```

### Requirements

- Must be safe to call frequently from Studio.
- Must return partial capability data when optional modules are absent.
- Must not fail the whole response when optional diagnostics data is unavailable; return capability state and omit/null that subsection.
- Must compute counts server-side using store count APIs where possible.
- Must cap any row sampling used for duration or diagnostics approximations and report approximation metadata if exact aggregation is not available.
- `workflowInstances.total` must equal the sum of top-level workflow status totals. `subStatuses.pending`, `executing`, `suspended`, and `interrupted` must reconcile to `statuses.running`; `subStatuses.finished`, `faulted`, and `cancelled` must reconcile to `statuses.finished`.
- `averageDurationMs` must be scoped to the selected range. It is the average of `FinishedAt - CreatedAt` for finished workflow instances whose `FinishedAt` falls within the response range. If exact aggregation is unavailable, the implementation may use a bounded sample filtered by `FinishedAt`; `averageDurationAccuracy.accuracy` must be `Sampled` unless the sample includes the full matching population.

## Endpoint 2: Workflow Trends

```http
POST /dashboard/workflow-trends
```

Returns bucketed workflow counts for charting.

### Request

```json
{
  "from": "2026-05-22T12:00:00Z",
  "to": "2026-05-23T12:00:00Z",
  "bucketSize": "PT1H",
  "includeSystem": false,
  "definitionIds": []
}
```

### Response

```json
{
  "buckets": [
    {
      "from": "2026-05-23T11:00:00Z",
      "to": "2026-05-23T12:00:00Z",
      "created": 51,
      "finished": 49,
      "faulted": 2,
      "cancelled": 0,
      "suspended": 4,
      "withIncidents": 2
    }
  ]
}
```

### Requirements

- Must enforce a maximum bucket count, for example 100.
- Must validate `from <= to`.
- Must reject unbounded requests.
- Buckets are half-open intervals: `from` is inclusive and `to` is exclusive, except the final bucket may include the request `to` instant.
- Bucket fields must use the following timestamp semantics:

| Field | Timestamp filter | Additional filter |
| --- | --- | --- |
| `created` | `CreatedAt` | None |
| `finished` | `FinishedAt` | `WorkflowStatus.Finished` and `WorkflowSubStatus.Finished` |
| `faulted` | `FinishedAt` | `WorkflowStatus.Finished` and `WorkflowSubStatus.Faulted` |
| `cancelled` | `FinishedAt` | `WorkflowStatus.Finished` and `WorkflowSubStatus.Cancelled` |
| `suspended` | `UpdatedAt` | `WorkflowStatus.Running` and `WorkflowSubStatus.Suspended` |
| `withIncidents` | `UpdatedAt` | Has one or more incidents |

- Initial implementation may use repeated `IWorkflowInstanceStore.CountAsync` calls.
- Future provider implementations may optimize with grouped SQL queries.

## Endpoint 3: Needs Attention

```http
GET /dashboard/needs-attention?range=PT24H&take=10&includeSystem=false
```

Returns prioritized operational findings.

### Finding Types

- Runtime is not accepting new work.
- Runtime ingress source is paused, stale, or failed.
- Faulted workflow instances.
- Interrupted workflow instances.
- Suspended workflow instances older than a threshold.
- Executing workflow instances stale beyond a configurable threshold.
- Workflow instances with incidents.
- Structured log errors or critical events in the selected range.
- Structured log storage dropped writes.
- Console stderr bursts.
- Console/structured log sources stale.
- Console/structured log dropped-line or dropped-event summaries.

### Response

```json
{
  "items": [
    {
      "id": "faulted-workflows",
      "severity": "Error",
      "title": "12 workflows faulted",
      "description": "12 workflow instances faulted in the last 24 hours.",
      "count": 12,
      "category": "Workflow",
      "target": {
        "type": "WorkflowInstances",
        "query": {
          "subStatus": "Faulted"
        }
      },
      "occurredAt": "2026-05-23T11:54:00Z"
    }
  ]
}
```

### Requirements

- Must return deterministic priority ordering: severity, count, recency, category.
- Must include enough target metadata for Studio to link to the relevant page/filter.
- `target.type` and `target.query` must follow this schema; query field names are camelCase and unknown fields are not allowed:

| `target.type` | Allowed `target.query` fields |
| --- | --- |
| `WorkflowInstances` | `status`, `subStatus`, `hasIncidents`, `definitionId`, `createdFrom`, `createdTo`, `updatedFrom`, `updatedTo`, `finishedFrom`, `finishedTo`, `includeSystem` |
| `WorkflowDefinitions` | `definitionId`, `includeSystem` |
| `RuntimeStatus` | `sourceName`, `sourceState` |
| `StructuredLogs` | `level`, `sourceName`, `from`, `to` |
| `ConsoleLogs` | `stream`, `sourceName`, `from`, `to` |

- Workflow target query fields should map directly to the existing workflow instance and definition list filter contracts where equivalent filters exist.
- `range` applies only to event findings with a timestamped occurrence in the selected window: faulted workflow instances, interrupted workflow instances, workflow instances with incidents updated in range, structured log errors or critical events, console stderr bursts, and dropped-line or dropped-event summaries when the diagnostics provider can report them by timestamp.
- Point-in-time findings ignore `range` and represent state at `generatedAt`: runtime not accepting work, runtime ingress source paused/stale/failed, suspended instances older than the configured threshold, executing instances stale beyond the configured threshold, storage dropped-write status, and stale diagnostics sources.
- Must not expose data from unauthorized optional capabilities.
- Must be bounded by `take`, with a maximum such as 50.

## Endpoint 4: Recent Activity

```http
GET /dashboard/recent-activity?take=20&includeSystem=false
```

Returns recent workflow instance summaries optimized for a compact dashboard table.

### Response Item

```json
{
  "workflowInstanceId": "abc",
  "definitionId": "order-flow",
  "definitionVersionId": "def",
  "name": "Order Flow",
  "version": 3,
  "status": "Finished",
  "subStatus": "Finished",
  "incidentCount": 0,
  "createdAt": "2026-05-23T11:58:00Z",
  "updatedAt": "2026-05-23T11:58:04Z",
  "finishedAt": "2026-05-23T11:58:04Z",
  "durationMs": 4000
}
```

### Requirements

- Must order by `UpdatedAt` descending by default.
- Must cap `take`, with default 20 and maximum 100.
- Should include duration when `FinishedAt` is present.
- Should not fetch full workflow state.

## Endpoint 5: Workflow Hotspots

```http
POST /dashboard/workflow-hotspots
```

Returns top workflow definitions by recent executions, fault count, incident count, and optionally average duration.

### Request

```json
{
  "from": "2026-05-22T12:00:00Z",
  "to": "2026-05-23T12:00:00Z",
  "take": 10,
  "includeSystem": false,
  "metric": "Faults"
}
```

### Response

```json
{
  "items": [
    {
      "definitionId": "order-flow",
      "name": "Order Flow",
      "executionCount": 842,
      "faultCount": 12,
      "incidentCount": 12,
      "averageDurationMs": 1380
    }
  ]
}
```

### Requirements

- Must support metrics: `Executions`, `Faults`, `Incidents`, `Duration`.
- Must cap `take`, with maximum 50.
- Must aggregate by logical workflow definition ID across all executed versions in the selected range.
- Must not include a `version` field in the initial response because the aggregate may span several definition versions. Version-specific drill-down can be added later as a separate endpoint or filter.
- `incidentCount` means the number of workflow instances in the selected range that have one or more incidents, not the sum of all individual incidents on those instances. If total incident occurrences are needed later, add a separate `incidentTotal` field.
- The `Duration` metric must not rank definitions from a single shared recent-summary sample. It requires provider-level grouped aggregation, or a two-stage sampled implementation that gathers a bounded per-definition sample, for example up to 100 finished instances per candidate definition, and reports sampled accuracy metadata.
- Initial implementation may sample recent summaries if aggregate grouping is not available, but response must expose whether values are exact or sampled.
- Future provider-specific optimizations should be possible without API changes.

## Data Sources and Computation

Use existing services first:

- `IWorkflowInstanceStore.CountAsync` for counts.
- `IWorkflowInstanceStore.SummarizeManyAsync` for recent rows and bounded sampling.
- `IWorkflowDefinitionStore.CountDistinctAsync` for logical definition count.
- A read-only `IWorkflowRuntimeStatusProvider.GetStatusAsync()` abstraction for runtime state. The provider may adapt existing runtime admin internals, but dashboard services and endpoints must not depend on `IWorkflowRuntimeAdminService` because that interface also exposes pause, resume, and drain operations.
- Diagnostics providers for recent/source/storage summaries when installed.
- Package/version service or existing package endpoint logic for version data.
- Installed feature provider for capability detection.
- When MultiTenancy is enabled, tenant scope must be resolved from the current request or shell context, for example through `ITenantAccessor` when registered, and applied to every workflow, runtime, and diagnostics query. Dashboard endpoints must not aggregate across tenants unless the caller is in an explicitly authorized cross-tenant administration context.

Where exact aggregation is not available, response models should include metadata such as:

```json
{
  "accuracy": "Sampled",
  "sampleSize": 500,
  "reason": "Store does not support grouped duration aggregation"
}
```

## Performance Requirements

- `GET /dashboard/overview` should complete within 500 ms for typical local and small production datasets when backed by indexed persistence.
- `POST /dashboard/workflow-trends` should complete within 750 ms for accepted requests on typical indexed persistence.
- `POST /dashboard/workflow-hotspots` should complete within 750 ms for accepted requests on typical indexed persistence.
- Trend requests should reject excessive bucket counts and excessive fallback query fan-out. The repeated-`CountAsync` implementation must reject or require a coarser bucket size when `bucketCount * countedFields` would exceed 200 store calls; provider-optimized grouped implementations may use the endpoint bucket cap directly.
- Hotspot requests must cap sampled rows, for example at 1,000 summaries, when exact grouped aggregation is unavailable.
- Endpoints must avoid loading full workflow state for aggregate cards.
- Endpoints must use cancellation tokens consistently.
- Repeated count queries should be parallelized server-side where safe.
- Any sampled list query must have a strict maximum page size.

## Security and Privacy Requirements

- All endpoints must require authorization except explicitly documented package/version reuse.
- Dashboard read permission must be separate from runtime management.
- Multi-tenant deployments must preserve tenant data isolation for every aggregate and summary field.
- Optional diagnostics subsections must obey diagnostics read permissions.
- Do not include raw workflow variables, inputs, outputs, or full workflow state.
- Do not include raw console lines in dashboard overview.
- Log-derived summaries must rely on already-redacted diagnostics providers.

## UX Contract With Studio

Studio should be able to render:

- Metric cards for running, completed, faulted, suspended, average duration, and warnings.
- Execution trend chart.
- Needs-attention list.
- Recent workflow activity table.
- Diagnostics snapshot.
- Runtime status chip and source status summary.
- Quick links to workflow instances, structured logs, and console logs.

The backend should return link target metadata where possible, but Studio remains responsible for actual route generation.

## Edge Cases

- No workflow persistence configured: return unavailable capability marker.
- Empty system: return zero counts and empty lists.
- Diagnostics modules absent: omit diagnostics summaries and mark capabilities unavailable.
- Diagnostics permission missing: mark capability unauthorized.
- Runtime admin service unavailable: mark runtime status unavailable.
- Multi-shell host: response should reflect the current shell context.
- Long-running request cancelled: stop store/diagnostics queries promptly.

## Acceptance Criteria

- A caller with dashboard read permission can fetch overview, trends, needs-attention, recent activity, and hotspots.
- Overview returns workflow counts, runtime summary, package version, capabilities, and optional diagnostics summaries.
- Trends return bounded, validated time buckets.
- Needs-attention returns prioritized findings with severity and link target metadata.
- Recent activity returns compact workflow instance rows without full workflow state.
- Hotspots return top workflow definitions for at least executions and faults.
- Runtime status summary is readable without granting runtime management permission.
- Optional diagnostics absence or authorization failure does not break workflow dashboard data.
- Unit tests cover request validation, permission/capability shaping, count aggregation, trend bucketing, and priority ordering.
- Integration tests verify endpoint registration and representative responses with in-memory stores.

## Open Questions

- Should dashboard endpoints require only `read:dashboard`, or both `read:dashboard` and underlying read permissions for linked workflow rows?
- Should the module live as `Elsa.Dashboard.Api` or under `Elsa.Workflows.Api` as dashboard endpoints?
