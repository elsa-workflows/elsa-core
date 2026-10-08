# Research: Live Server Log Streaming

## R1: Raw console capture vs structured logging

**Decision**: Capture `ILogger` events and render them console-style in Studio.

**Rationale**: Elsa and ASP.NET Core already use `ILogger`; structured events preserve level, category, scopes, exception data, correlation, and workflow/tenant context. Raw `Console.Out` cannot reliably support filtering, redaction, or cluster source identity.

**Alternatives considered**:

- Redirect `Console.Out`: simple but fragile, global, hard to secure, and loses structured properties.
- Require Serilog/Seq/Loki: powerful but would make the feature dependent on a third-party logging stack.

## R2: SignalR vs Server-Sent Events

**Decision**: Use SignalR for live streaming plus REST for backfill.

**Rationale**: Studio already has SignalR authentication configuration via `IHttpConnectionOptionsConfigurator`, and Core already has a SignalR precedent for workflow instance updates.

**Alternatives considered**:

- SSE: simpler one-way transport but would need separate auth and reconnect conventions.
- Polling only: easiest but loses the Aspire-like live tail experience.

## R3: Cluster support

**Decision**: Model source identity in the MVP and expose a provider abstraction. Ship in-memory provider first; add shared providers later.

**Rationale**: Kubernetes support is best achieved through a shared log provider or existing observability backend. The UI contract should not depend on whether events come from memory, Redis, OpenTelemetry, Loki, or another store.

**Alternatives considered**:

- Direct Kubernetes API reads: useful for pods but too deployment-specific and unsuitable for non-Kubernetes clusters.
- Single-node only: simpler but would bake in the wrong event model.

## R4: Redaction timing

**Decision**: Redact before buffering and before publishing.

**Rationale**: Recent buffers may be exposed later to authorized users. Keeping raw secrets in memory increases accidental disclosure risk.

## R5: Source health

**Decision**: Track source `LastSeen` and classify health as connected, stale, or disconnected based on provider data and configured timeout.

**Rationale**: This works for in-memory and cluster providers without requiring a control-plane integration.

## R6: Ordering merged streams

**Decision**: Sort recent queries by event timestamp with sequence/receive order as a deterministic tiebreaker. Live streams deliver provider order.

**Rationale**: Cluster clocks can skew. A stable tiebreaker is more important than pretending perfect global ordering exists.
