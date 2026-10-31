# Elsa Studio Weaver

`Elsa.Studio.AI` adds the Weaver workspace to Studio.

Studio talks only to Elsa Core-owned endpoints:

- `GET /ai/capabilities`
- `GET /ai/tools`
- `POST /ai/chat` as `text/event-stream`

The module intentionally does not reference provider SDKs or call providers from the browser. Proposal events are rendered when they appear in the stream, but review/apply buttons remain disabled until Core exposes proposal action endpoints. Steering and server-side queueing are represented in the state model and capability panel, but the current Core API does not expose endpoints for either capability yet.
