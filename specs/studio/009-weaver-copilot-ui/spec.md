# Feature Specification: Weaver Copilot UI

**Feature Branch**: `009-weaver-copilot-ui`  
**Created**: 2026-06-08  
**Status**: Draft  
**Input**: User description: "Build a proper Elsa Studio UI for interacting with the Weaver copilot backend. The UI must use Elsa Core provider-agnostic contracts, support streaming assistant output, tool/proposal activity, context references, capability discovery, reconnect/resume, optional steering and queuing where exposed, tests, and documentation. Studio must not depend on provider SDK types or bypass Elsa Core APIs."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Work With Weaver In Studio (Priority: P1)

An authenticated Studio user opens Weaver from Studio navigation and uses a dense operational workspace to ask for help with Elsa workflows without leaving Studio.

**Why this priority**: The primary value of the feature is making Weaver available as a real Studio workspace instead of a generic chat box.

**Independent Test**: Can be tested by opening the Weaver workspace, sending a message, observing assistant output, and confirming the interface only exposes Elsa-owned concepts.

**Acceptance Scenarios**:

1. **Given** Weaver is available for the current backend, **When** a user opens Studio navigation, **Then** the user can open a Weaver workspace as the first screen for the feature.
2. **Given** a user is in the Weaver workspace, **When** the user starts a new chat turn, **Then** the message appears in the conversation and the assistant response streams progressively.
3. **Given** Weaver is unavailable or the user lacks access, **When** the user opens the route directly, **Then** Studio shows a clear unavailable or unauthorized state without exposing unsupported controls.

---

### User Story 2 - Understand Agent Activity (Priority: P1)

As Weaver works, a Studio user can see long-running activity, tool usage, results, and abstract reasoning or planning signals so the user understands progress without provider internals.

**Why this priority**: Agentic work can take time and invoke tools; users need operational visibility to trust and manage the session.

**Independent Test**: Can be tested by replaying streamed activity events and verifying that assistant text, tool status, tool results, reasoning activity, and errors render as scannable timeline items.

**Acceptance Scenarios**:

1. **Given** Weaver emits tool activity, **When** a tool starts, completes, fails, or returns a result, **Then** Studio displays the tool name, status, summary, and result state using provider-neutral labels.
2. **Given** Weaver emits reasoning or agent activity, **When** those events arrive, **Then** Studio displays abstract activity summaries without raw provider-specific event names.
3. **Given** streaming is interrupted, **When** the connection fails, **Then** Studio preserves visible conversation state and offers a safe reconnect or retry path where supported.

---

### User Story 3 - Attach Elsa Context (Priority: P2)

A Studio user can attach relevant Elsa context, especially workflow definitions and workflow instances, before asking Weaver for help.

**Why this priority**: Weaver is most useful when grounded in the workflow object the user is reviewing or changing.

**Independent Test**: Can be tested by selecting workflow definitions and workflow instances, sending a message, and confirming the selected references are represented as context references rather than copied provider prompts.

**Acceptance Scenarios**:

1. **Given** workflow definitions exist, **When** a user attaches a workflow definition, **Then** the attachment appears as a removable context reference in the composer.
2. **Given** workflow instances exist, **When** a user attaches a workflow instance, **Then** the attachment appears as a removable context reference in the composer.
3. **Given** a context reference is no longer wanted, **When** the user removes it before sending, **Then** it is not included in the next turn.

---

### User Story 4 - Review And Apply Proposals (Priority: P2)

A Studio user can review Weaver-generated workflow proposals, approve or reject them, and apply approved proposals through governed backend actions.

**Why this priority**: Workflow modification is a high-value but sensitive capability and must remain auditable and controlled.

**Independent Test**: Can be tested with proposal events and proposal actions by verifying proposal status display, review controls, and apply/reject outcomes.

**Acceptance Scenarios**:

1. **Given** Weaver emits a proposal event, **When** the event arrives, **Then** Studio displays the proposal summary, affected workflow reference, status, and available actions.
2. **Given** a proposal is reviewable, **When** the user approves or rejects it, **Then** Studio calls the governed backend action and updates the displayed status.
3. **Given** a proposal is approved and apply is available, **When** the user applies it, **Then** Studio submits the apply request through the backend and shows success, failure, or pending status.

---

### User Story 5 - Resume And Steer Longer Work (Priority: P3)

A Studio user can resume active conversations and provide guidance during an agent turn when the backend exposes those capabilities.

**Why this priority**: Reconnect, queueing, and steering improve long-running work, but the UI must not invent unavailable backend behavior.

**Independent Test**: Can be tested by changing discovered capabilities and verifying that resume, interrupt, guidance, and pending message affordances appear only when supported.

**Acceptance Scenarios**:

1. **Given** the backend exposes active or resumable conversations, **When** Studio loads Weaver, **Then** the user can reconnect to supported active sessions.
2. **Given** the backend exposes steering, **When** an agent turn is running, **Then** the user can provide supported guidance, interruption, clarification, or preference updates.
3. **Given** the backend does not expose steering or queueing, **When** an agent turn is running, **Then** Studio disables or hides unsupported controls while preserving a state model that can support them later.

### Edge Cases

- Weaver capability discovery returns no supported tools, proposals, steering, queueing, or resume support.
- The current backend changes while a Weaver conversation is active.
- A streaming event arrives out of expected order or with an unknown event type.
- A proposal action is denied, already applied, expired, or superseded.
- Context references point to workflow definitions or instances the user can no longer access.
- A pending local message exists while a turn is active and the backend does not expose server-side queueing.
- The user navigates away and returns while a conversation is still active.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Studio MUST provide a Weaver workspace that is directly usable as the feature's first screen.
- **FR-002**: Studio MUST discover Weaver availability and supported capabilities from the backend before enabling Weaver-specific controls.
- **FR-003**: Studio MUST hide, disable, or mark unavailable any control whose required backend capability is not advertised.
- **FR-004**: Users MUST be able to start a new chat turn with a text message.
- **FR-005**: Studio MUST display streamed assistant output incrementally as it arrives.
- **FR-006**: Studio MUST display tool activity and tool result states in a provider-neutral timeline.
- **FR-007**: Studio MUST display reasoning or agent activity when exposed by the backend while avoiding raw provider names, SDK types, and provider event labels.
- **FR-008**: Users MUST be able to attach workflow definition and workflow instance context references to a turn.
- **FR-009**: Studio MUST display attached context references as explicit, removable items before a turn is sent.
- **FR-010**: Studio MUST support reconnecting or resuming active conversations when the backend advertises that capability.
- **FR-011**: Studio MUST preserve local conversation state during transient streaming failures and present the safest available recovery action.
- **FR-012**: Studio MUST display proposal events with proposal identity, affected context, summary, current status, and available user actions.
- **FR-013**: Users MUST be able to approve, reject, and apply proposals only through backend-provided proposal actions.
- **FR-014**: Studio MUST reflect proposal action outcomes, including pending, success, failure, denied, expired, or already-applied states.
- **FR-015**: Studio MUST support steering controls only when backend capabilities or stream/input contracts expose them.
- **FR-016**: Studio MUST support queue or pending-turn controls only when backend capabilities or contracts expose them.
- **FR-017**: When server-side queueing is unavailable, Studio MAY show local pending-message affordances only if they do not imply that a turn has been accepted by the backend.
- **FR-018**: Studio MUST avoid direct browser-to-provider communication.
- **FR-019**: Studio MUST NOT import, reference, display, or persist provider SDK types.
- **FR-020**: Studio MUST use Elsa-owned backend contracts for chat, stream, context, capability, tool, proposal, resume, steering, and queue states.
- **FR-021**: Studio MUST include tests that cover chat rendering, streamed events, tool and proposal states, reconnect behavior, and disabled capability states.
- **FR-022**: Studio MUST document any backend gaps that prevent steering or queueing from being fully supported.

### Key Entities *(include if feature involves data)*

- **Weaver Conversation**: A resumable user interaction with Weaver, containing turns, activity events, proposal references, state, and backend-owned identifiers.
- **Chat Turn**: A user message and related assistant response lifecycle, including active, completed, failed, cancelled, queued, or pending states where supported.
- **Stream Event**: A provider-neutral event from the backend that updates assistant text, activity, tool status, proposal status, turn state, errors, or reconnect state.
- **Tool Activity**: A visible unit of agent work that includes a display name, status, optional input or result summary, timing, and error state.
- **Context Reference**: A backend-resolvable reference to an Elsa object such as a workflow definition or workflow instance.
- **Workflow Proposal**: A governed change suggestion created by Weaver and managed through review, approval, rejection, and apply status.
- **Capability Descriptor**: Backend-provided information that determines which Weaver controls, activities, tools, proposals, steering, queueing, and resume features Studio may expose.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can open Weaver, attach a workflow reference, send a message, and see the first assistant activity within 5 seconds on a healthy backend.
- **SC-002**: At least 95% of supported streamed event types render as recognizable conversation, activity, tool, proposal, or status items during test replay.
- **SC-003**: Unsupported steering, queueing, proposal, and resume controls are disabled or hidden in 100% of capability-disabled test cases.
- **SC-004**: A user can identify active tool work, completed tool results, failed tool work, and proposal status in under 10 seconds during usability review.
- **SC-005**: Provider-specific SDK names and types appear zero times in Studio source models, UI labels, and persisted state for this feature.
- **SC-006**: Automated tests cover the primary chat flow, streaming updates, tool states, proposal actions, reconnect state, and capability-gated controls.

## Assumptions

- Weaver is exposed by Elsa Core through authenticated Studio-facing HTTP and streaming contracts.
- Studio users are already authenticated through existing Studio mechanisms.
- Elsa Core owns authorization, tenancy, redaction, governed tool execution, proposal safety, audit, and provider integration.
- The first supported context reference types are workflow definitions and workflow instances.
- Unknown stream events should be preserved as non-blocking activity or ignored safely rather than breaking the conversation.
- If backend support for steering or queueing is absent, this feature should document the gap and avoid unsupported calls.
