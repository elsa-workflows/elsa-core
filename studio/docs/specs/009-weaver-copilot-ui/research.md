# Research: Weaver Copilot UI

## Elsa Core AI Contract

Elsa Core `release/3.8.0` exposes provider-neutral Studio APIs for Weaver:

- `GET /ai/capabilities`
- `GET /ai/tools`
- `POST /ai/chat` returning `text/event-stream`

The Studio UI maps these contracts into local Weaver models and avoids provider SDK types. The backend owns provider selection, agent/session/tool continuation, RBAC, tenancy, governed execution, redaction, proposal safety, and audit.

## Streaming

Core stream events use provider-neutral event names such as `conversation.started`, `assistant.delta`, `assistant.completed`, `tool.started`, `tool.progress`, `tool.result`, `proposal.created`, `proposal.updated`, `conversation.completed`, and `conversation.error`. The Studio reducer treats unknown event types as non-blocking activity.

## Backend Gaps

The current Core API does not expose proposal action endpoints for approve, reject, or apply. Studio renders proposal events and disables actions with an explicit tooltip.

The current Core API does not expose steering endpoints or capability flags. Studio models steering as a future capability and keeps controls disabled.

The current Core API does not expose server-side queueing or pending-turn endpoints. Studio does not claim backend acceptance for queued turns.
