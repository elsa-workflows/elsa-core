# Data Model: Weaver Copilot UI

## WeaverCapabilities

Backend discovery result with streaming, conversation persistence, proposal review, supported attachment kinds, and agents.

## WeaverChatRequest

Provider-neutral chat request with optional conversation id, user message, optional Elsa-owned agent name, and context attachments.

## WeaverStreamEvent

Provider-neutral SSE event with type, conversation id, sequence, timestamp, and JSON payload.

## WeaverWorkspaceState

In-memory UI state containing conversation id, busy/reconnect/error flags, messages, activity items, proposal items, attachments, and pending local message state.

## WeaverActivityItem

Scannable timeline item for conversation state, assistant activity, tool calls, tool results, errors, and unknown events.

## WeaverProposalItem

Visible proposal summary with backend-owned proposal id, status, kind, summary, workflow reference, timestamp, and raw provider-neutral payload.

## WeaverContextAttachment

Backend-resolvable Elsa context reference. Initial supported kinds are workflow definitions and workflow instances when advertised by Core.
