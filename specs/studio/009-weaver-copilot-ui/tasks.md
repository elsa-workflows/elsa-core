# Tasks: Weaver Copilot UI

**Input**: Design documents from `/specs/009-weaver-copilot-ui/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

## Phase 1: Setup

- [x] T001 Create `src/modules/Elsa.Studio.AI` module project.
- [x] T002 Add AI module to `Elsa.Studio.sln`.
- [x] T003 Register AI module in Server and WASM hosts.
- [x] T004 Add AI module reference to the Studio bundle.

## Phase 2: Foundation

- [x] T005 Add provider-neutral Weaver API client contracts for `/ai/capabilities` and `/ai/tools`.
- [x] T006 Add provider-neutral Weaver chat, capability, tool, stream event, attachment, proposal, and workspace models.
- [x] T007 Add source-generated JSON context for stream/request serialization.
- [x] T008 Add remote Weaver service with unavailable-backend handling.
- [x] T009 Add SSE stream client for `POST /ai/chat`.
- [x] T010 Add workspace reducer for streamed event state transitions.

## Phase 3: User Story 1 - Work With Weaver

- [x] T011 Add Weaver feature gate and menu item.
- [x] T012 Add `/ai/weaver` workspace page.
- [x] T013 Implement new conversation, send turn, streaming assistant output, and safe error display.
- [x] T014 Add capability-gated composer and unavailable backend state.

## Phase 4: User Story 2 - Understand Agent Activity

- [x] T015 Render provider-neutral activity timeline.
- [x] T016 Render tool activity and tool result states.
- [x] T017 Render proposal events and disabled action state for missing backend proposal endpoints.
- [x] T018 Preserve state on streaming interruption and expose reconnect when persistence is advertised.

## Phase 5: User Story 3 - Attach Elsa Context

- [x] T019 Add context reference attachments for backend-advertised attachment kinds.
- [x] T020 Render removable context reference chips in the composer.
- [x] T021 Include context references in chat turn requests.

## Phase 6: Steering, Queueing, and Capability Gaps

- [x] T022 Gate steering and queueing controls from backend capabilities.
- [x] T023 Keep unsupported steering and queueing disabled without inventing API calls.
- [x] T024 Document steering, queueing, and proposal-action backend gaps.

## Phase 7: Tests and Verification

- [x] T025 Add reducer tests for assistant deltas, tools, proposals, reconnect, disabled capabilities, and completion.
- [x] T026 Add serialization tests for capabilities and stream events.
- [x] T027 Add menu feature gate tests.
- [x] T028 Run focused tests and host builds.
