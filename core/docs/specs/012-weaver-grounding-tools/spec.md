# Feature Specification: Weaver Grounding Tools

**Feature Branch**: `codex/weaver-grounding-tools`
**Created**: 2026-06-08
**Status**: Draft
**Input**: User description: "Plan the use cases and tools needed to ground Weaver/Copilot in Elsa data such as installed activity metadata, workflow definitions, workflow instances, incidents, and proposal-based workflow authoring."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Discover available activities (Priority: P1)

A workflow author asks Weaver what activities are available or which activity should be used for a desired step, and Weaver answers using the activities installed in the current Elsa server.

**Why this priority**: Workflow creation and update quality depends on knowing the real Activity Registry, not a generic model memory of possible activities.

**Independent Test**: Ask Weaver to find an activity by capability, category, or input/output shape, then verify the answer only includes activities visible to the current user and tenant.

**Acceptance Scenarios**:

1. **Given** a server with installed activities, **When** a user asks what can receive an HTTP request, **Then** Weaver identifies matching installed activities with concise metadata and usage notes.
2. **Given** multiple versions of an activity exist, **When** Weaver requests details for the activity, **Then** the response clearly identifies version, inputs, outputs, categories, and constraints.
3. **Given** an activity is not available to the tenant or is not browsable, **When** Weaver searches activities, **Then** the activity is excluded or clearly marked unavailable according to policy.

---

### User Story 2 - Understand workflow definitions (Priority: P2)

A user asks Weaver to explain, search, compare, or inspect workflow definitions and receives answers grounded in authorized workflow definition data.

**Why this priority**: Users need confidence that Weaver understands existing workflows before trusting generated changes.

**Independent Test**: Attach or search for a workflow definition, ask Weaver to explain it, and verify the answer uses the actual workflow graph, activities, variables, inputs, outputs, triggers, and version metadata.

**Acceptance Scenarios**:

1. **Given** a user can view a workflow definition, **When** they ask Weaver what it does, **Then** Weaver summarizes triggers, activities, data flow, branches, and external dependencies.
2. **Given** a user searches for workflows using an activity, **When** matching workflows exist, **Then** Weaver returns only authorized matches with enough context to choose one.
3. **Given** a workflow has multiple versions, **When** Weaver compares versions, **Then** it explains meaningful graph and metadata differences without leaking unauthorized data.

---

### User Story 3 - Create and update workflows safely (Priority: P3)

A workflow author asks Weaver to create or update a workflow, and Weaver uses installed activity descriptors and validation tools to produce a reviewable proposal instead of directly saving changes.

**Why this priority**: This is the core agentic authoring use case, but it must remain governed and reviewable.

**Independent Test**: Ask Weaver to create or update a workflow that uses installed activities, verify it creates a proposal with validation diagnostics, and verify no workflow is persisted until approved and applied.

**Acceptance Scenarios**:

1. **Given** a user can create workflows, **When** they ask Weaver to create a workflow, **Then** Weaver consults available activities and creates a proposal with a valid draft, rationale, warnings, and validation diagnostics.
2. **Given** a user asks to modify an existing workflow, **When** Weaver proposes the change, **Then** the proposal references the baseline workflow version and includes a reviewable graph diff.
3. **Given** a proposed draft uses an unavailable activity or invalid input, **When** validation runs, **Then** the proposal is blocked with actionable diagnostics.

---

### User Story 4 - Inspect runtime instances and incidents (Priority: P4)

An operator asks Weaver why a workflow failed or what happened in a workflow instance, and Weaver uses authorized runtime data to summarize state, history, variables, incidents, and likely causes.

**Why this priority**: Runtime inspection makes Weaver useful beyond authoring and gives operators fast, evidence-backed support.

**Independent Test**: Ask Weaver to inspect a failed instance, then verify the response identifies the workflow, failed activity, incident/error evidence, relevant state, and next investigation steps.

**Acceptance Scenarios**:

1. **Given** a user can view a workflow instance, **When** they ask what happened, **Then** Weaver summarizes status, timeline, current/failed activity, incidents, and relevant variables.
2. **Given** a user asks for recurring failures in a time range, **When** matching incidents exist, **Then** Weaver summarizes trends within the selected scope only.
3. **Given** runtime data contains sensitive values, **When** Weaver reads or reports it, **Then** sensitive values are redacted before model context, stream output, and audit storage.

---

### User Story 5 - Surface grounding capabilities to Studio (Priority: P5)

Elsa Studio discovers which Weaver grounding features are available and renders chat controls, context attachment options, tool activity, proposal review, and unsupported-state messaging accordingly.

**Why this priority**: Studio must remain provider-agnostic while still giving users a useful agentic interface.

**Independent Test**: Request Weaver capabilities from Studio, verify supported attachment kinds and tool families are advertised, and verify UI controls are enabled only when backend capabilities exist.

**Acceptance Scenarios**:

1. **Given** Activity Registry grounding is available, **When** Studio loads Weaver capabilities, **Then** it can offer activity-aware authoring and activity search affordances.
2. **Given** runtime diagnostics tools are disabled, **When** Studio loads Weaver capabilities, **Then** instance/incident analysis controls are hidden or disabled with an explanatory state.
3. **Given** a chat turn invokes tools, **When** stream events arrive, **Then** Studio can render tool activity and proposal state without knowing provider SDK details.

### Edge Cases

- Activity metadata is missing, oversized, localized, duplicated, or has multiple versions.
- A workflow references a custom activity that is no longer installed.
- A user asks for tenant-wide analysis without selecting a permitted scope.
- Workflow definitions or instances are deleted between context selection and tool execution.
- A proposal baseline becomes stale before apply.
- Runtime variables, inputs, outputs, logs, incidents, or activity metadata contain secrets or sensitive configuration.
- A tool returns too many matches for model context and must paginate or summarize.
- The Copilot provider is unavailable while grounding tools and capability endpoints remain available.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide activity discovery tools that query installed activity metadata from Elsa's Activity Registry.
- **FR-002**: Activity discovery MUST support search by name, display name, category, namespace/type, version, input/output name, trigger capability, and free-text terms.
- **FR-003**: Activity detail results MUST include model-safe metadata for inputs, outputs, description, categories, version, browsability, trigger capability, and usage constraints.
- **FR-004**: System MUST provide workflow definition tools for search, retrieval, graph summary, version metadata, activity usage, and version comparison.
- **FR-005**: Workflow definition tools MUST enforce tenant, ownership, and workflow read permissions before returning data.
- **FR-006**: System MUST provide workflow proposal tools for creating drafts, updating existing workflows, validating drafts, and comparing drafts to baselines.
- **FR-007**: AI-originated workflow creation and update MUST remain proposal-only until a user explicitly approves and applies the proposal.
- **FR-008**: System MUST validate proposed workflow drafts against installed activity descriptors, workflow graph rules, required inputs, expression compatibility where practical, and baseline version.
- **FR-009**: System MUST provide runtime tools for searching instances, retrieving instance summaries, reading execution history, reading activity state, reading variables safely, and finding incidents.
- **FR-010**: Runtime tools MUST require an explicit workflow, instance, diagnostics scope, or time range unless the user has an administrative analysis permission.
- **FR-011**: System MUST redact sensitive values before data is sent to Copilot, streamed to Studio, or written to audit records.
- **FR-012**: Tool results MUST be bounded and summarizable so large activity catalogs, workflow graphs, logs, or incident sets do not exceed configured context limits.
- **FR-013**: System MUST expose provider-neutral capabilities that identify available grounding tool families and supported context attachment kinds.
- **FR-014**: Studio-facing contracts MUST not expose GitHub Copilot SDK types or database entities.
- **FR-015**: Every grounding tool invocation MUST be audited with actor, tenant, conversation, tool name, status, and redacted summary.
- **FR-016**: System MUST support deterministic tool outputs suitable for Copilot SDK tool callbacks and Studio tool activity rendering.
- **FR-017**: System MUST document which grounding tools are read-only, proposal-only, or future administrative actions.
- **FR-018**: Initial implementation MUST exclude direct destructive actions such as delete workflow, cancel instance, restart instance, or bulk retry.

### Key Entities *(include if feature involves data)*

- **Activity Grounding Summary**: Model-safe view of an installed activity descriptor, including type, version, name, description, categories, inputs, outputs, trigger behavior, and constraints.
- **Workflow Grounding Summary**: Model-safe view of a workflow definition or version, including identity, status, graph shape, activities used, variables, inputs, outputs, and links to full authorized details.
- **Workflow Draft Proposal**: Reviewable AI-generated workflow creation or update package with baseline, draft payload, diff, rationale, warnings, and validation diagnostics.
- **Runtime Instance Summary**: Model-safe view of a workflow instance, including status, workflow reference, timeline, current activity, incidents, selected variables, and redacted inputs/outputs.
- **Grounding Tool Result**: Bounded, redacted response from a Weaver tool with result items, summary, paging/cursor hints, and evidence references.
- **Grounding Capability Descriptor**: Provider-neutral capability advertised to Studio so UI features can be enabled or disabled.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Weaver can answer activity discovery questions using installed Activity Registry data in seeded tests with no hallucinated activity names.
- **SC-002**: Weaver can create a proposal for a simple workflow using only installed activities and receives blocking diagnostics when an unavailable activity is requested.
- **SC-003**: Weaver can explain a seeded workflow definition with correct trigger, activity, and data-flow references.
- **SC-004**: Weaver can inspect a seeded failed workflow instance and identify the failed activity, primary error, and relevant incident evidence.
- **SC-005**: All grounding tool responses are redacted and stay within configured result size limits in tests with oversized metadata or runtime data.
- **SC-006**: Studio capability discovery can determine whether activity, workflow, proposal, and runtime grounding are available without provider-specific assumptions.

## Assumptions

- The Copilot SDK integration from `specs/008-weaver-ai-copilot` is already present and owns the agent loop.
- Elsa Server remains the only component allowed to access workflow stores, runtime stores, Activity Registry, diagnostics, and audit persistence.
- Studio sends references and user intent only; it does not send raw workflow or runtime data to an AI provider.
- MVP covers read-only grounding tools and proposal-only workflow mutations; direct operational actions are deferred.
- Existing Elsa authorization, tenancy, activity registry, workflow management, runtime, and diagnostics abstractions remain the source of truth.
