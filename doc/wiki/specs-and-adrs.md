# Specs And ADRs

The repository carries two useful design-history systems:

- ADRs in [doc/adr](../adr), which document durable architecture decisions.
- Spec Kit feature specs in [specs](../../specs), which document planned and recently implemented feature work.

Use both before making architectural changes. Specs often explain the "why now"; ADRs explain decisions intended to outlive a single feature.

## ADR Index

The table of contents is [doc/adr/toc.md](../adr/toc.md).

Current ADRs:

| ADR | Topic |
| --- | --- |
| [0001](../adr/0001-record-architecture-decisions.md) | Record architecture decisions. |
| [0002](../adr/0002-fault-propagation-from-child-to-parent-activities.md) | Fault propagation from child to parent activities. |
| [0003](../adr/0003-direct-bookmark-management-in-workflowexecutioncontext.md) | Direct bookmark management in `WorkflowExecutionContext`. |
| [0004](../adr/0004-activity-execution-snapshots.md) | Activity execution snapshots. |
| [0005](../adr/0005-token-centric-flowchart-execution-model.md) | Token-centric flowchart execution. |
| [0006](../adr/0006-tenant-deleted-event.md) | Tenant deleted event. |
| [0007](../adr/0007-adoption-of-explicit-merge-modes-for-flowchart-joins.md) | Explicit merge modes for flowchart joins. |
| [0008](../adr/0008-empty-string-as-default-tenant-id.md) | Empty string as default tenant ID. |
| [0009](../adr/0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md) | Asterisk sentinel value for tenant-agnostic entities. |
| [0010](../adr/0010-default-admin-user-bootstrap-for-initial-identity-access.md) | Default admin user bootstrap for initial identity access. |

## Active And Recent Specs

| Spec | Area | Why it matters |
| --- | --- | --- |
| [001 shell reload API](../../specs/001-shell-reload-api/spec.md) | Shell management | Explains reload behavior for modular/shell hosts. |
| [002 graceful shutdown](../../specs/002-graceful-shutdown/spec.md) | Runtime | Defines quiescence, ingress sources, drain orchestration, interrupted recovery, and runtime admin endpoints. |
| [003 live server logs](../../specs/003-live-server-logs/spec.md) | Diagnostics precursor | Earlier live server logs work that led to structured diagnostics. |
| [004 diagnostics structured logs](../../specs/004-diagnostics-structured-logs/spec.md) | Diagnostics | Refactors server logs into structured log diagnostics with semantic `ILogger` capture. |
| [005 structured log persistence](../../specs/005-structured-log-persistence/spec.md) | Diagnostics persistence | Adds storage abstraction, relational persistence, SQLite durability, migrations, write queue, and retention. |
| [006 diagnostics console logs](../../specs/006-diagnostics-console-logs/spec.md) | Diagnostics | Defines capture, buffering, endpoints, SignalR hub, permissions, source identity, and redaction for raw console output. |
| [006 state machine activity](../../specs/006-state-machine-activity/spec.md) | Workflow core | Adds a state machine activity with named states and trigger-driven transitions to the workflow engine. |
| [007 secrets module](../../specs/007-secrets-module/spec.md) | Secrets | Revamps the secrets module with named secrets, pluggable stores, extensible secret types, secret picker UX, permissions, import/export encryption support, and migration from existing sensitive fields. |
| [009 operational dashboard](../../specs/009-operational-dashboard/prd.md) | Dashboard API | PRD for a read-only backend dashboard API module exposing workflow activity aggregates, health signals, and operational summaries without requiring Studio to orchestrate many separate requests. |

Each spec folder usually contains:

- `spec.md`: product/user-facing requirements
- `plan.md`: architecture and implementation plan
- `research.md`: decisions and tradeoffs
- `data-model.md`: domain model
- `contracts`: API/provider contracts
- `quickstart.md`: usage validation
- `tasks.md`: implementation backlog
- `checklists/requirements.md`: requirement quality checks

## Reading Order For Runtime Work

For runtime behavior, read in this order:

1. [Workflow Runtime wiki page](workflow-runtime.md)
2. [specs/002-graceful-shutdown/plan.md](../../specs/002-graceful-shutdown/plan.md)
3. [ADR 0003](../adr/0003-direct-bookmark-management-in-workflowexecutioncontext.md)
4. [ADR 0004](../adr/0004-activity-execution-snapshots.md)
5. affected runtime service and tests

## Reading Order For Flowchart Work

1. [Workflow Core wiki page](workflow-core.md)
2. [ADR 0005](../adr/0005-token-centric-flowchart-execution-model.md)
3. [ADR 0007](../adr/0007-adoption-of-explicit-merge-modes-for-flowchart-joins.md)
4. [Flowchart activities](../../src/modules/Elsa.Workflows.Core/Activities/Flowchart/Activities)
5. flowchart unit/integration tests

## Reading Order For Tenancy Work

1. [Identity, Tenancy, And Security](identity-tenancy-security.md)
2. [ADR 0008](../adr/0008-empty-string-as-default-tenant-id.md)
3. [ADR 0009](../adr/0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md)
4. tenant feature and persistence code
5. tenant unit tests

## Reading Order For Diagnostics Work

1. [Diagnostics Structured Logs](diagnostics-structured-logs.md)
2. [specs/004-diagnostics-structured-logs/plan.md](../../specs/004-diagnostics-structured-logs/plan.md)
3. [specs/005-structured-log-persistence/plan.md](../../specs/005-structured-log-persistence/plan.md)
4. structured logs core package
5. relational and SQLite persistence packages
6. structured logs unit/integration tests

## Reading Order For Secrets Work

1. [Identity, Tenancy, And Security](identity-tenancy-security.md) — Secrets section
2. [specs/007-secrets-module/spec.md](../../specs/007-secrets-module/spec.md)
3. [specs/007-secrets-module/plan.md](../../specs/007-secrets-module/plan.md)
4. `Elsa.Secrets` feature and contracts
5. secrets unit tests

## When To Write An ADR

Write or update an ADR when a decision:

- changes workflow execution semantics
- changes persisted data conventions
- changes tenant/security behavior
- introduces a durable architectural boundary
- rejects an obvious alternative that future contributors may ask about
- affects multiple modules or provider packages

Feature-specific decisions can stay in `specs/*/research.md` unless they are expected to outlive the feature or guide unrelated future work.
