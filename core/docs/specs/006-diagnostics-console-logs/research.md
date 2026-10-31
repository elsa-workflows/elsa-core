# Research: Diagnostics Console Logs

## Decision: Create a separate diagnostics console logs module

**Rationale**: Raw stdout/stderr capture has different semantics, security risks, and provider boundaries than structured `ILogger` records. A focused `Elsa.Diagnostics.ConsoleLogs` module keeps structured logs, console logs, and future OpenTelemetry exploration independently deployable and understandable.

**Alternatives considered**:

- Extend `Elsa.Diagnostics.StructuredLogs`: rejected because it would mix raw console capture with semantic logging.
- Add console capture to app hosts only: rejected because Studio needs a stable Core feature contract.

## Decision: Use an in-process capture tee as the default capture boundary

**Rationale**: The first slice must work for local development, tests, and single-node hosts while preserving host stdout/stderr behavior. A tee can observe writes, keep the original console destination active, and feed redaction/provider pipelines.

**Alternatives considered**:

- Replace stdout/stderr without teeing: rejected because it would break host-visible console output.
- Use orchestrator log APIs first: rejected as out of scope and provider-specific.

## Decision: Keep line-oriented events with buffered partial writes

**Rationale**: The Studio-facing contract is simpler and safer when events are complete lines. Buffering partial writes until newline, max line length, or idle timeout avoids fragment assembly in clients while still exposing long-running partial output.

**Alternatives considered**:

- Stream fragments immediately: rejected because clients would need assembly state and fragment contracts.
- Drop partial writes: rejected because shutdown and long-running progress output could be lost.

## Decision: Truncate oversized lines and strip ANSI by default

**Rationale**: A single truncated event with a truncation flag preserves bounded memory and simple ordering. Stripping ANSI by default avoids leaking terminal control sequences to browser clients while still allowing host opt-in preservation.

**Alternatives considered**:

- Split oversized lines into chunks: rejected because it adds chunk metadata and client reassembly complexity.
- Preserve ANSI by default: rejected because browser rendering and security expectations should start from plain text.

## Decision: Redact before provider storage, streaming, or source listing

**Rationale**: Providers may later become shared or external. Keeping raw unredacted content inside the capture/redaction boundary prevents accidental persistence or cross-node exposure and matches the authorization posture.

**Alternatives considered**:

- Let providers receive raw content internally: rejected because provider implementations would each need to prove redaction safety.
- Make redaction provider-specific: rejected because it weakens the Core contract.

## Decision: Use REST backfill/source endpoints plus SignalR live subscriptions

**Rationale**: Existing diagnostics structured logs already use REST for recent queries and source listing, and SignalR for live mutable subscriptions. Mirroring that split keeps Studio integration predictable and allows bounded backfill before live streaming.

**Alternatives considered**:

- Server-sent events only: rejected because the repository already has SignalR patterns for authenticated live diagnostics.
- WebSocket-only backfill and live data: rejected because source listing and recent queries are simpler and more testable over REST endpoints.

## Decision: Keep external aggregators and durable retention out of scope

**Rationale**: The spec requires source identity and provider boundaries, not direct Kubernetes, Docker, vendor sink, durable audit, or OpenTelemetry integrations. A bounded in-memory provider satisfies the first Core slice without speculative packages.

**Alternatives considered**:

- Add SQLite persistence now: rejected because recent console history is operational troubleshooting data, not durable audit logging.
- Add Kubernetes/Docker providers now: rejected because external provider requirements need separate specs.
