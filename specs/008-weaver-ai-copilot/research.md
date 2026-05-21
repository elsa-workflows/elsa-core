# Research: Weaver AI Copilot Platform

## Decision: Studio remains AI-agnostic

**Rationale**: Studio should render chat, stream events, tool progress, and proposals, but it must not host model runtimes, carry provider credentials, or send raw workflow/runtime data to providers. Server-side context resolution preserves tenancy, RBAC, redaction, and audit.

**Alternatives considered**:

- Direct Studio-to-provider calls: rejected because it leaks provider details, credentials, and raw data into the SPA.
- Embedding provider SDKs in Studio: rejected because it couples UI releases to volatile AI runtime APIs.

## Decision: Elsa Server hosts Weaver orchestration

**Rationale**: Server hosting lets Elsa enforce tenant scope, permissions, proposal lifecycle, validation, redaction, audit, and telemetry before tools or models see data.

**Alternatives considered**:

- External standalone agent service: deferred because it adds deployment and authorization complexity before the Elsa-owned governance model is proven.
- Reimplementing a generic agent runtime: rejected because the product goal is Elsa-specific governance and workflow intelligence, not a new general-purpose runtime.

## Decision: Isolate Copilot integration behind `Elsa.AI.Copilot`

**Rationale**: GitHub documents the Copilot SDK as public preview, with functionality subject to change. The adapter boundary protects Elsa abstractions, Studio contracts, and workflow models from provider SDK churn while still using Copilot runtime features.

**Sources**: [GitHub Copilot SDK custom agents](https://docs.github.com/en/copilot/how-tos/copilot-sdk/use-copilot-sdk/custom-agents), [SDK and CLI compatibility](https://docs.github.com/en/copilot/how-tos/copilot-sdk/troubleshooting/sdk-and-cli-compatibility)

**Alternatives considered**:

- Expose Copilot SDK types in server contracts: rejected because it would make Elsa public APIs depend on preview runtime details.
- Build provider-specific behavior into `Elsa.AI.Host`: rejected because provider isolation is a core architectural requirement.

## Decision: Translate provider events into Elsa stream events

**Rationale**: Copilot SDK emits assistant deltas, tool execution lifecycle events, permission/user-input events, session lifecycle events, and sub-agent events. Elsa should map those into stable `AiStreamEvent` contracts for Studio.

**Sources**: [Streaming events in the Copilot SDK](https://docs.github.com/en/enterprise-cloud%40latest/copilot/how-tos/copilot-sdk/use-copilot-sdk/streaming-events)

**Alternatives considered**:

- Pass provider event envelopes directly to Studio: rejected because it leaks provider-specific event names and fields.
- Buffer full responses only: rejected because the PRD requires streaming deltas and tool progress.

## Decision: Use proposal-only workflow mutations

**Rationale**: Workflow creation and edits must be reviewable, validated, auditable, and applied only after user approval. This keeps AI from directly mutating production artifacts and lets Elsa own workflow semantics.

**Alternatives considered**:

- AI tools save definitions directly after validation: rejected because validation does not replace user review and approval.
- Client-side proposal apply: rejected because apply must enforce server authorization, stale baseline checks, validation, and audit.

## Decision: Built-in tools use Elsa abstractions only

**Rationale**: Tools must respect existing workflow stores, instance stores, activity catalog services, diagnostics sources, authorization, and tenancy. Direct persistence access would bypass existing boundaries and make provider tools harder to govern.

**Alternatives considered**:

- Tool-specific repository access: rejected because it duplicates existing service contracts and weakens authorization boundaries.
- Prompt-only context injection: rejected because large datasets should be queried through scoped tools rather than dumped into prompts.

## Decision: Require durable proposal and audit storage, with configurable conversation retention

**Rationale**: Workflow proposals and audit records are governance artifacts, so MVP must persist them durably. Conversation and session history can remain retention-configurable because chat transcript durability is less critical than proposal review, approval, application, and audit evidence.

**Alternatives considered**:

- In-memory proposals and audit for MVP: rejected because proposal review and audit evidence must survive server restarts.
- Durable conversations, proposals, and audit in MVP: partially rejected because conversation history can be configurable without weakening workflow mutation governance.
- Provider-owned conversation persistence only: rejected because Elsa must audit and govern conversations independently of provider internals.

## Decision: Govern MCP and custom agents as extension registrations

**Rationale**: Copilot supports custom agents with scoped tools and MCP servers. Elsa should expose module registration APIs that declare scope, permissions, mutability, danger level, and audit policy before any external tool becomes available.

**Sources**: [GitHub Copilot SDK custom agents](https://docs.github.com/en/copilot/how-tos/copilot-sdk/use-copilot-sdk/custom-agents), [GitHub Copilot CLI command reference](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-command-reference)

**Alternatives considered**:

- Let operators pass arbitrary MCP configuration from Studio: rejected because it bypasses governance.
- Make all tools available to every agent: rejected because least-privilege tool scoping is required for safe delegation.

## Decision: Enable tools by mutability and integration risk

**Rationale**: Read-only module tools are low-risk when existing module permissions and tenant checks apply. Proposal, administrative, and MCP-backed tools can affect workflow changes or external integrations, so they require explicit administrator enablement before use.

**Alternatives considered**:

- Auto-enable every registered tool: rejected because MCP and proposal tools need administrator review.
- Require manual enablement for every read-only module tool: rejected because it adds friction for low-risk module capabilities already protected by permissions.

## Decision: Continue disconnected chat turns for a grace window

**Rationale**: A transient browser disconnect should not immediately abandon a useful AI turn, but the server should not run expensive provider work indefinitely without an attached client. A configurable grace window balances user experience and resource control.

**Alternatives considered**:

- Cancel immediately on disconnect: rejected because it makes streaming fragile.
- Always run to completion after disconnect: rejected because it can waste provider and server resources.

## Decision: Scope runtime trend analysis to explicit user-selected context

**Rationale**: Trend analysis is useful only when broad enough to inspect multiple failures, but unrestricted tenant-wide analysis risks unexpected data exposure and large prompt/tool workloads. Attached references plus explicit time range and diagnostics scope keep access intentional and auditable.

**Alternatives considered**:

- Only inspect attached instances: rejected because trend analysis needs a bounded set of related runtime data.
- Inspect any readable tenant runtime data: rejected because it is too broad by default.

## Decision: Use the `elsa-extensions` AI prototype as UI precedent, not as final UX

**Rationale**: Branch `origin/feat/ai` at commit `93f0e09d71e57f5daff1e2d593f0a51faaa80417` contains useful Studio patterns for `/ai/*` navigation, Agents menu placement, dense management tables, API clients, validators, and agent editor tabs. Weaver should reuse those interaction patterns where they fit Elsa Studio conventions, but the primary UX must be chat and proposal review rather than CRUD-first agent/provider administration.

**Prototype elements to consider**:

- `src/modules/agents/Elsa.Studio.Agents/AgentsMenu.cs` for `/ai/agents`, `/ai/api-keys`, and `/ai/services` menu organization.
- `UI/Pages/Agents.razor` for dense table, bulk actions, row navigation, and create action patterns.
- `UI/Pages/Agent.razor` for tabbed agent configuration: General, Input, Output, Services, Plugins, and Execution Settings.
- `Client/IAgentsApi.cs`, `IApiKeysApi.cs`, `IServicesApi.cs`, and validators for Studio API client and form validation structure.

**Alternatives considered**:

- Copy the prototype UI as-is: rejected because Weaver's MVP is chat/proposal review, and the prototype exposes API key values and provider-specific service settings too directly.
- Ignore the prototype: rejected because it captures useful Elsa Studio route, menu, table, dialog, and editor conventions for AI-related surfaces.
