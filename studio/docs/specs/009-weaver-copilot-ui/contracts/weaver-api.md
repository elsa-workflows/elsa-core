# Weaver API Contracts

Studio uses only Elsa Core-owned HTTP contracts.

## `GET /ai/capabilities`

Returns Weaver capability discovery:

- `streaming`
- `conversationPersistence`
- `proposalReview`
- `supportedAttachmentKinds`
- `agents`

Controls are gated from this response.

## `GET /ai/tools`

Returns provider-neutral tool descriptors including name, display name, mutability, danger level, permissions, tenancy behavior, audit behavior, scopes, and enabled state.

## `POST /ai/chat`

Accepts:

- `conversationId`
- `message`
- `agent`
- `attachments`

Returns `text/event-stream` with provider-neutral `WeaverStreamEvent` JSON in SSE `data:` frames.

## Unsupported in Current Core API

No proposal review/apply endpoints, steering endpoints, or server-side queue endpoints are currently exposed. Studio does not invent calls for these behaviors.
