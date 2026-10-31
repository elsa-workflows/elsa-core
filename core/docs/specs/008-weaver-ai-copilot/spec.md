# Feature Specification: Weaver AI Copilot Platform

**Feature Branch**: `codex/008-weaver-ai-copilot`  
**Created**: 2026-05-20  
**Status**: Draft  
**Input**: User description: "Implement Weaver, an AI assistant / agent that provides agentic workflow authoring and management similar to GitHub Copilot, integrated as a chat interface in Elsa Studio."

## Clarifications

### Session 2026-05-20

- Q: What durability level should Weaver require for MVP conversations, proposals, and audit records? → A: Durable proposals and audit required; conversation history retention configurable.
- Q: Should Weaver require separation of duties between the user who requests an AI proposal and the user who approves/applies it? → A: Same authorized user may request, approve, and apply proposals.
- Q: When third-party modules or MCP servers contribute Weaver tools, should those tools become available automatically? → A: Auto-enable read-only module tools; require explicit enablement for proposal, administrative, and MCP tools.

### Session 2026-05-21

- Q: What should Weaver do when chat stream clients disconnect during an in-progress turn? → A: Continue the turn for a short grace window, persist durable outputs, and allow reconnect.
- Q: For runtime trend analysis, what data scope may Weaver inspect by default? → A: Attached references plus user-selected time range and diagnostics scope.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Chat with workflow-aware Weaver (Priority: P1)

An Elsa Studio user opens a Weaver panel, attaches workflow or runtime references, asks a question, and receives a streaming answer with visible tool progress and results.

**Why this priority**: Conversational access is the foundation for every authoring, diagnostics, and operations use case.

**Independent Test**: Start a chat with a workflow definition reference, verify the server resolves context, streams message deltas, emits tool progress, and completes the conversation without Studio contacting an AI provider directly.

**Acceptance Scenarios**:

1. **Given** a user with permission to view a workflow definition, **When** they ask Weaver to explain that workflow, **Then** Weaver streams an answer grounded in server-resolved workflow context.
2. **Given** a chat request with context attachment references, **When** the server receives the request, **Then** the server resolves only the authorized data needed for the turn and redacts sensitive values before model context is built.
3. **Given** a tool is invoked during a chat turn, **When** the tool starts, progresses, returns, or fails, **Then** Studio receives stream events that can render the tool lifecycle.

---

### User Story 2 - Generate workflows through reviewable proposals (Priority: P2)

A workflow author describes a desired workflow in natural language and receives a structured workflow proposal with rationale, validation diagnostics, warnings, and graph diff before any workflow is persisted.

**Why this priority**: Safe creation is the first high-value authoring workflow and proves the proposal-only mutation model.

**Independent Test**: Ask Weaver to create a workflow, inspect the proposal, validate it, approve it, and verify the workflow is applied only after approval and audit entries are recorded.

**Acceptance Scenarios**:

1. **Given** a user can create workflows, **When** they ask Weaver to generate one, **Then** Weaver creates a proposal with workflow payload, rationale, warnings, validation diagnostics, graph preview, and proposal ID.
2. **Given** a generated proposal has validation errors, **When** the user attempts to apply it, **Then** the server blocks application and returns the validation diagnostics.
3. **Given** a valid proposal is approved, **When** the user applies it, **Then** the server persists the workflow and records the approval, actor, tenant, and applied change.

---

### User Story 3 - Edit and validate existing workflows safely (Priority: P3)

A workflow author asks Weaver to modify, explain, or validate an existing workflow and receives proposed changes with an understandable graph diff and risk summary.

**Why this priority**: Editing existing workflows is more common and riskier than greenfield creation, so it builds on the proven proposal lifecycle.

**Independent Test**: Attach an existing workflow, ask Weaver for a targeted change, verify the returned diff is reviewable, validation runs before apply, and direct AI persistence is impossible.

**Acceptance Scenarios**:

1. **Given** a user can edit a workflow, **When** they ask Weaver to add or change workflow behavior, **Then** Weaver returns a proposal rather than directly saving changes.
2. **Given** a proposed edit may break existing behavior, **When** validation runs, **Then** the proposal includes warnings and blocking diagnostics that Studio can display.
3. **Given** a user asks why a workflow behaves a certain way, **When** Weaver analyzes the definition, **Then** the answer explains structure, activities, inputs, outputs, and likely execution path without creating a proposal.

---

### User Story 4 - Analyze runtime incidents conversationally (Priority: P4)

An operator attaches workflow instance, diagnostics, log, or time-range references and asks Weaver to summarize failures, identify failing activities, inspect state, and highlight recurring trends.

**Why this priority**: Runtime intelligence reduces operational friction and turns existing Elsa telemetry into actionable guidance.

**Independent Test**: Provide failed instance and log references, ask for an incident summary, and verify Weaver uses read-only tools to produce a grounded summary with failing activities, evidence, and next investigation steps.

**Acceptance Scenarios**:

1. **Given** a user can inspect a workflow instance, **When** they ask Weaver why it failed, **Then** Weaver summarizes errors, activity state, relevant history, and probable causes using authorized runtime data.
2. **Given** attached runtime references plus a user-selected time range and diagnostics scope with multiple failures, **When** the user asks for trends, **Then** Weaver identifies recurring workflows, activities, error categories, and time patterns within that selected scope.
3. **Given** runtime data includes secrets or sensitive configuration, **When** Weaver builds context or returns results, **Then** sensitive values are redacted.

---

### User Story 5 - Extend Weaver with governed tools and agents (Priority: P5)

A module author contributes tools, context providers, custom agents, or external tool integrations that are scoped by permissions, tenant behavior, danger level, mutability, and audit policy.

**Why this priority**: Weaver must become a platform capability rather than a closed feature owned only by the core AI module.

**Independent Test**: Register a module-provided read-only tool and a proposal tool, verify they appear in capabilities, execute only when authorized, and emit audit and telemetry records.

**Acceptance Scenarios**:

1. **Given** a module registers an AI tool, **When** Weaver capabilities are requested, **Then** the tool is listed with schema, permissions, mutability, danger level, tenant behavior, and audit behavior.
2. **Given** a tool requires permissions the current user lacks, **When** an agent attempts to invoke it, **Then** the server denies execution and records the denial.
3. **Given** a custom agent is registered with a scoped tool set, **When** Weaver delegates to that agent, **Then** the agent can only access its allowed tools and context providers.
4. **Given** a module registers proposal, administrative, or MCP-backed tools, **When** the module is enabled, **Then** those tools remain unavailable until explicitly enabled by an authorized administrator.

### Edge Cases

- Chat requests reference deleted, inaccessible, or cross-tenant workflow data.
- Streaming clients that disconnect mid-turn can reconnect during a configurable grace window and recover durable outputs produced while disconnected.
- Tool execution exceeds limits, fails validation, or returns more data than can be safely included in model context.
- Proposal payloads become stale because the workflow changed after the proposal was created.
- Same-actor proposal approval and application still require explicit user action and audit records.
- A third-party tool declares unsafe metadata or attempts to bypass server authorization.
- Provider runtime is unavailable, misconfigured, or incompatible with the configured adapter version.
- Users ask Weaver to reveal secrets, credentials, or sensitive configuration.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a Weaver chat experience in Elsa Studio that communicates only with Elsa Server APIs.
- **FR-002**: Studio MUST send context references, not raw workflow, runtime, diagnostic, tenant, or activity data.
- **FR-003**: Server MUST resolve context attachments according to tenant scope, ownership, and user permissions.
- **FR-004**: System MUST stream assistant message deltas, tool progress, tool results, proposal notifications, errors, and completion events.
- **FR-005**: System MUST support configurable conversation and session history retention; conversation durability is configurable for MVP.
- **FR-006**: System MUST expose capabilities and tool metadata so Studio can render available Weaver behavior.
- **FR-007**: AI-originated workflow creation and edit output MUST be represented as proposals, never direct persisted workflow mutations.
- **FR-008**: Workflow proposals MUST include structured workflow payload, rationale, warnings, validation diagnostics, graph diff or preview, and proposal ID.
- **FR-009**: Server MUST validate proposals before they can be applied.
- **FR-010**: The same authorized user MUST be able to request, approve, reject, and apply proposals in MVP.
- **FR-011**: System MUST block proposal application when validation fails or when the source workflow revision no longer matches the proposal baseline.
- **FR-012**: System MUST durably audit prompts, model/session events, tool calls, proposal diffs, approvals, rejections, applied changes, timestamps, actors, and tenant IDs.
- **FR-013**: System MUST provide built-in read-only workflow tools for getting definitions, listing definitions, getting instances, searching instances, and reading the activity catalog.
- **FR-014**: System MUST provide built-in proposal tools for proposing workflow creation, proposing workflow updates, and validating draft workflow payloads.
- **FR-015**: Runtime diagnostics tools MUST support failure inspection, incident summaries, failing activity identification, execution history review, and trend analysis.
- **FR-016**: Tools MUST execute server-side and use published Elsa abstractions rather than bypassing authorization or direct persistence boundaries.
- **FR-017**: Third-party modules MUST be able to register AI tools, context providers, custom agents, and external tool integrations.
- **FR-018**: Every registered tool MUST declare schema, danger level, mutability, permissions, tenant behavior, and audit behavior.
- **FR-019**: System MUST support local and remote external tool servers with allowlists, per-agent scoping, and explicit administrator enablement.
- **FR-020**: System MUST keep AI provider/runtime details isolated from Studio contracts, workflow models, and Elsa core abstractions.
- **FR-021**: System MUST support provider-agnostic configuration with a bring-your-own-key model where applicable.
- **FR-022**: System MUST support telemetry for trace correlation, tool execution metrics, model/session events, and proposal lifecycle metrics.
- **FR-023**: System MUST redact secrets and sensitive configuration before data enters model context, tool results, streamed events, audit records, or proposal rationale.
- **FR-024**: Administrative mutation tools such as tenant modification, workflow deletion, or instance termination MUST remain out of MVP scope.
- **FR-025**: Read-only tools registered by enabled modules MAY be available by default, but proposal, administrative, and MCP-backed tools MUST require explicit administrator enablement before use.
- **FR-026**: System MUST continue in-progress chat turns for a configurable disconnect grace window, persist durable outputs produced during that window, and allow authorized clients to reconnect to recover them.
- **FR-027**: Runtime trend analysis MUST be limited by default to attached references plus an explicit user-selected time range and diagnostics scope.

### Key Entities *(include if feature involves data)*

- **AI Conversation**: A user-visible Weaver chat thread with messages, participants, tenant, timestamps, context attachment references, provider session reference, and configurable retention behavior.
- **AI Session**: Server-managed runtime session used to execute agent turns, stream events, invoke tools, and resume conversations.
- **Context Attachment**: A reference to workflow, runtime, activity, tenant, diagnostic, or time-range context that the server resolves safely.
- **AI Tool Definition**: Registered capability with schema, permissions, mutability, danger level, tenant behavior, audit behavior, and agent scope.
- **Tool Invocation**: A single attempted tool execution with arguments, authorization result, progress, output summary, telemetry, and audit metadata.
- **Workflow Proposal**: A durable reviewable AI-generated workflow creation or edit package with baseline, payload, rationale, warnings, diagnostics, graph diff, state, and apply result.
- **AI Audit Event**: Durable record of prompt, context resolution, tool invocation, proposal lifecycle, approval, application, or denial.
- **AI Agent Definition**: Named behavior profile with prompt, allowed tools, allowed context providers, external integrations, and governance metadata.
- **External Tool Server Registration**: Governed local or remote integration endpoint with allowlist, agent scoping, and audit policy.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An authorized user can start a Weaver chat with workflow context and receive first streamed output within 3 seconds under normal server load.
- **SC-002**: At least 95% of valid workflow generation requests in MVP produce a reviewable proposal with payload, rationale, diagnostics, and graph preview without directly persisting changes.
- **SC-003**: Proposal application is blocked 100% of the time when validation fails, authorization fails, or the workflow baseline is stale.
- **SC-004**: Runtime incident analysis can identify the failed workflow instance, failing activity, primary error, and relevant execution evidence for seeded test incidents.
- **SC-005**: Third-party modules can register at least one read-only tool and one proposal tool without changing core Weaver orchestration code.
- **SC-006**: Studio never sends raw workflow/runtime datasets or provider credentials to an AI provider directly.
- **SC-007**: All tool executions and proposal lifecycle transitions create audit records with actor and tenant metadata.
- **SC-008**: Provider-specific runtime types are absent from Elsa core abstractions, Studio contracts, and workflow definition models.

## Assumptions

- Weaver is the product name for the Elsa Studio AI assistant and server-hosted AI platform surface.
- The first implementation targets Elsa Server plus a paired Elsa Studio module; code changes in the Studio repository will be tracked separately if that repository is not present in this workspace.
- Existing Elsa identity, tenancy, workflow definition, workflow instance, activity catalog, diagnostics, logging, and validation services remain the source of truth.
- MVP supports workflow authoring proposals and read-only runtime diagnostics; administrative destructive actions are deferred.
- Conversation history retention is configurable for MVP, while workflow proposals and audit records require durable persistence.
- Provider runtime integration is isolated behind a module boundary so preview or unstable provider APIs can change without breaking Elsa contracts.
