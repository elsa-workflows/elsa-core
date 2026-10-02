# Research: Diagnostics Console Logs Studio Module

## Decision: Add a separate ConsoleLogs diagnostics module

**Decision**: Implement `Elsa.Studio.Diagnostics.ConsoleLogs` as a new module beside `Elsa.Studio.Diagnostics.StructuredLogs`.

**Rationale**: Console streaming answers a different question from structured logs: raw stdout/stderr process output instead of semantic `ILogger` records. A separate module keeps route ownership, feature gating, contracts, tests, and UI vocabulary clear.

**Alternatives considered**:

- Extend StructuredLogs with a Console tab: rejected because it would mix raw stdout/stderr with structured log records and weaken the spec's separation requirement.
- Rename StructuredLogs again: rejected because StructuredLogs already owns semantic log inspection and must remain separate.

## Decision: Reuse Studio diagnostics and authentication patterns

**Decision**: Follow the existing diagnostics module shape: module under `src/modules/`, `Feature.cs` with a remote feature name, Diagnostics navigation entry, Refit-style API client through `IBackendApiClientProvider`, and SignalR connection authentication through `IHttpConnectionOptionsConfigurator`.

**Rationale**: The constitution requires backend capability awareness and existing Studio abstractions. The current StructuredLogs module already demonstrates the feature registration, menu, API client, and authenticated SignalR patterns to reuse.

**Alternatives considered**:

- Direct `HttpClient`/manual token handling: rejected because it bypasses Studio environment and authentication abstractions.
- A shared diagnostics base module: deferred until at least two diagnostics modules have concrete duplication that warrants extraction.

## Decision: Use diagnostics console logs REST and SignalR paths

**Decision**: Use `/diagnostics/console-logs/recent` and `/diagnostics/console-logs/sources` for REST client contracts, and build the live SignalR hub as `new Uri(backendUrl, "hubs/diagnostics/console-logs")`.

**Rationale**: These paths align with the existing `/diagnostics/structured-logs` convention while preserving `/diagnostics/console` as the user-facing Studio route. With the default backend API base ending in `/elsa/api`, the relative hub URL resolves to `/elsa/hubs/diagnostics/console-logs`, matching the Structured Logs hub pattern. The `console-logs` segment stays explicit for backend endpoints and avoids ambiguity with generic console UI.

**Alternatives considered**:

- `/diagnostics/console/recent`: rejected because the Studio route already uses `/diagnostics/console`, and backend API naming should describe the resource.
- `/server-logs` compatibility paths: rejected for this feature because ConsoleLogs is new and should not inherit old server-log identity.

## Decision: Hide navigation but keep direct route states

**Decision**: Hide the Diagnostics `Console` navigation item when the backend remote feature or permission is unavailable, while direct visits to `/diagnostics/console` render unavailable or unauthorized states.

**Rationale**: This avoids advertising unsupported tools while still making copied/bookmarked URLs deterministic and safe. It also matches the clarified spec and constitution guidance for missing backend capabilities.

**Alternatives considered**:

- Always show disabled navigation: rejected because it adds noise to the operational navigation for unsupported backends.
- Redirect away from direct routes: rejected because it hides the reason a shared URL cannot be used.

## Decision: Server-side text filtering for recent and live data

**Decision**: Send `text` to both recent backfill requests and live stream subscriptions, and highlight matching text in returned visible lines.

**Rationale**: Console output can be high volume. Server-side filtering reduces irrelevant data sent to Studio and keeps live subscriptions aligned with shared URL state.

**Alternatives considered**:

- Client-filter live streams only: rejected because the backend contract already supports text filtering and high-volume streams would still burden the client.
- Client-filter everything: rejected because it would make no-match states and large backfills less predictable.

## Decision: Stable source identity with display metadata

**Decision**: Use stable backend `source.id` for source filters, SignalR subscriptions, URL state, and stale-source selection. Use `displayName` and metadata only for labels/details.

**Rationale**: Source display names, pods, containers, and process metadata can change independently of identity. Stable IDs keep bookmarks and active subscriptions valid across metadata refreshes.

**Alternatives considered**:

- Filter by display name: rejected because names are not guaranteed unique or stable.
- Filter by composite metadata: rejected because different deployment providers expose different metadata.

## Decision: URL state is explicit and shareable

**Decision**: Preserve `source`, `stream`, `text`, `from`, `to`, `wrap`, `compact`, `ansi`, and `follow` in the query string, with `from` and `to` as ISO-8601 UTC timestamps.

**Rationale**: Explicit parameters are readable, testable, and stable for bookmarks. UTC timestamps avoid timezone-dependent interpretations when URLs are shared between users.

**Alternatives considered**:

- Short query names: rejected because readability is more useful than a shorter URL here.
- Relative time ranges: rejected for the first slice because shared URLs should restore the same time window deterministically.

## Decision: Local export only for the first slice

**Decision**: Copy and export operate on the current visible rows in Studio's local buffer.

**Rationale**: The spec intentionally keeps backend-generated downloads outside Studio's first slice. Local export is enough for the viewer's inspection workflow and can be implemented entirely in the Studio module.

**Alternatives considered**:

- Backend export endpoint: deferred to a future backend-owned spec.
- Export all matching backend lines: rejected because it implies backend pagination/download semantics not covered by this Studio feature.
