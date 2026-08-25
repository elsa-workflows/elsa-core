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
| [0011](../adr/0011-output-conversion-at-binding-is-synchronous.md) | Output conversion occurs synchronously at the binding boundary. |
| [0012](../adr/0012-output-converters-use-explicit-stable-identities.md) | Output converters use explicit stable identities. |
| [0013](../adr/0013-output-converter-discovery-is-server-owned.md) | Output converter discovery is server-owned. |
| [0014](../adr/0014-broker-external-sign-in-through-elsa-server.md) | Broker external sign-in through Elsa Server. |
| [0015](../adr/0015-compose-a-scoped-connection-registry.md) | Compose a scoped connection registry. (Partially superseded by 0021.) |
| [0016](../adr/0016-extend-authentication-through-deployed-descriptor-providers.md) | Extend authentication through deployed descriptor providers. (V1 portion superseded by 0022.) |
| [0017](../adr/0017-separate-external-identity-from-elsa-authorization.md) | Separate external identity from Elsa authorization. (V1 portion superseded by 0022.) |
| [0018](../adr/0018-separate-provider-trust-from-broker-invariants.md) | Separate provider trust from broker invariants. (Manual-endpoint portion superseded by 0024.) |
| [0019](../adr/0019-bind-sessions-to-shared-state-and-connection-revisions.md) | Bind sessions to shared state and connection revisions. (Identity portion superseded by 0021.) |
| [0020](../adr/0020-publish-audit-ready-security-notifications.md) | Publish audit-ready security notifications. |
| [0021](../adr/0021-identify-host-connections-by-logical-key-and-use-explicit-overrides.md) | Identify host connections by logical key and use explicit overrides. |
| [0022](../adr/0022-match-unlinked-identities-with-trusted-user-matchers.md) | Match unlinked identities with trusted user matchers. |
| [0023](../adr/0023-separate-authentication-ui-composition-from-security-administration.md) | Separate authentication UI composition from security administration. |
| [0024](../adr/0024-use-exact-oidc-discovery-and-deployment-derived-callbacks.md) | Use exact OIDC discovery and deployment-derived callbacks. |
| [0025](../adr/0025-two-axis-authorization-model.md) | Two-axis authorization model with open resources and open verbs. |
| [0026](../adr/0026-identity-neutral-user-task-participants.md) | Use identity-neutral participant references for User Tasks. |
| [0027](../adr/0027-project-user-tasks-from-committed-bookmarks.md) | Project User Tasks from committed workflow bookmarks. |
| [2026-08-25](../adr/2026-08-25-date-prefixed-adr-identifiers.md) | Identify new ADRs by date instead of a sequential number. |

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
| [008 diagnostics OpenTelemetry](../../specs/008-diagnostics-otel/spec.md) | Diagnostics | Defines the first-party OTLP ingestion backend, trace/metric/log storage and query APIs, live SignalR streaming, and Studio-facing telemetry investigation. |
| [008 Weaver AI copilot](../../specs/008-weaver-ai-copilot/spec.md) | AI | Implements Weaver, an agentic workflow assistant with streaming chat, context resolution, reviewable proposals, audit, and Studio integration. |
| [009 operational dashboard](../../specs/009-operational-dashboard/prd.md) | Dashboard API | PRD for a read-only backend dashboard API module exposing workflow activity aggregates, health signals, and operational summaries without requiring Studio to orchestrate many separate requests. |
| [010 workflow JSON hardening](../../specs/010-workflow-json-hardening/spec.md) | Workflow core | Introduces dedicated type aliases for workflow JSON, rejects unknown/unsafe CLR names, and preserves backward-compatible reads for selected legacy identifiers. |
| [011 persistence vNext](../../specs/011-persistence-vnext/spec.md) | Persistence | Provider-neutral module-owned storage manifests, portable document/index store, relational and MongoDB physicalization, and schema versioning without per-provider migration packages. |
| [012 output converters](../../specs/012-output-converters/spec.md) | Workflow core | Extensible, explicitly-identified output converters that transform an activity's native output at the binding boundary before writing the destination variable or workflow output. |
| [012 external authentication](../../specs/012-external-authentication/spec.md) | Security | Server-brokered external identity providers: Identity Provider Connections, OpenID Connect adapter, linked identity resolution, configurable unlinked-identity policies, and EF Core persistence across all providers. |
| [012 weaver grounding tools](../../specs/012-weaver-grounding-tools/spec.md) | AI | Grounds Weaver in real Elsa server data: activity registry discovery, workflow definition inspection, instance and incident investigation, and proposal-based workflow authoring with validation. |
| [013 RBAC authorization model](../../specs/013-rbac-authorization-model/spec.md) | Security | Replaces the ad-hoc permission vocabulary with a structured two-axis model (`resource:verb`), a module-contributed catalog, a single evaluator, wildcard grants, and automated endpoint-declaration gate. |
| [013 user tasks](../../specs/013-user-tasks/spec.md) | Workflow core | Durable, identity-neutral, workflow-bound human tasks: task queue, forms, guest invitations, due-date handling, EF Core and VNext persistence, Studio workbench, and a reconciler for interrupted projection. |

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

## Reading Order For Output Converters Work

1. [Output Converters wiki page](output-converters.md)
2. [ADR 0011](../adr/0011-output-conversion-at-binding-is-synchronous.md)
3. [ADR 0012](../adr/0012-output-converters-use-explicit-stable-identities.md)
4. [ADR 0013](../adr/0013-output-converter-discovery-is-server-owned.md)
5. [specs/012-output-converters/plan.md](../../specs/012-output-converters/plan.md)
6. output converter contracts and implementation in `Elsa.Workflows.Core`

## Reading Order For AI Copilot Work

1. [specs/008-weaver-ai-copilot/spec.md](../../specs/008-weaver-ai-copilot/spec.md)
2. [specs/008-weaver-ai-copilot/plan.md](../../specs/008-weaver-ai-copilot/plan.md)
3. `Elsa.AI.Abstractions` contracts
4. `Elsa.AI.Host` feature and endpoints
5. `Elsa.AI.Copilot` adapter and options
6. AI unit and integration tests

## Reading Order For External Authentication Work

1. [specs/012-external-authentication/spec.md](../../specs/012-external-authentication/spec.md)
2. [specs/012-external-authentication/plan.md](../../specs/012-external-authentication/plan.md)
3. [Identity, Tenancy, And Security](identity-tenancy-security.md)
4. `Elsa.ExternalAuthentication` feature and contracts
5. `Elsa.ExternalAuthentication.OpenIdConnect` adapter
6. `Elsa.ExternalAuthentication.Persistence.EFCore` and provider packages

## Reading Order For Authorization Work

1. [Identity, Tenancy, And Security](identity-tenancy-security.md) — API Authorization section
2. [ADR 0025](../adr/0025-two-axis-authorization-model.md)
3. [specs/013-rbac-authorization-model/spec.md](../../specs/013-rbac-authorization-model/spec.md)
4. [doc/migrations/authorization-model.md](../migrations/authorization-model.md)
5. `Elsa.Permissions` core contracts (`Permission`, `CoreVerbs`, `IPermissionEvaluator`, `PermissionDescriptor`)
6. `Elsa.Identity` endpoints (`GET /identity/permissions`, `/identity/permissions/reach`, `/identity/me/permissions`)

## Reading Order For User Tasks Work

1. [User Tasks](user-tasks.md)
2. [ADR 0026](../adr/0026-identity-neutral-user-task-participants.md)
3. [ADR 0027](../adr/0027-project-user-tasks-from-committed-bookmarks.md)
4. [specs/013-user-tasks/spec.md](../../specs/013-user-tasks/spec.md)
5. [specs/013-user-tasks/contracts/runtime-contract.md](../../specs/013-user-tasks/contracts/runtime-contract.md)
6. `Elsa.UserTasks` feature, contracts, and activities
7. `test/component` user tasks persistence conformance tests

## Reading Order For BPMN Work

1. [bpmn-workflows.md](bpmn-workflows.md)
2. `Elsa.Bpmn/Activities/BpmnProcess.cs` — the scope activity
3. `Elsa.Bpmn/Hosting/BpmnWorkLedger.cs` and `BpmnWorkBinder.cs` — work tracking and binding
4. `Elsa.Bpmn.Interchange/Binding/BpmnActivityBindingFormat.cs` — `elsa:` vendor extension
5. `Elsa.Bpmn.Interchange/Features/BpmnInterchangeFeature.cs` — feature registration
6. `test/integration/Elsa.Bpmn.IntegrationTests` and `Elsa.Bpmn.Interchange.IntegrationTests`

## Reading Order For Persistence vNext Work

1. [Persistence wiki page](persistence.md) — Persistence vNext section
2. [specs/011-persistence-vnext/spec.md](../../specs/011-persistence-vnext/spec.md)
3. [specs/011-persistence-vnext/roadmap.md](../../specs/011-persistence-vnext/roadmap.md)
4. `Elsa.Persistence.VNext` core abstractions
5. `Elsa.Persistence.VNext.Relational`, `Sqlite`, `PostgreSql`, `SqlServer`, `MongoDb` providers

## When To Write An ADR

Write or update an ADR when a decision:

- changes workflow execution semantics
- changes persisted data conventions
- changes tenant/security behavior
- introduces a durable architectural boundary
- rejects an obvious alternative that future contributors may ask about
- affects multiple modules or provider packages

Feature-specific decisions can stay in `specs/*/research.md` unless they are expected to outlive the feature or guide unrelated future work.
