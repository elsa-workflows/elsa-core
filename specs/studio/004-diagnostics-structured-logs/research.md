# Research: Diagnostics Structured Logs Studio Module

## Decision: Rename the module instead of adding a second module

**Decision**: Rename `Elsa.Studio.ServerLogs` to `Elsa.Studio.Diagnostics.StructuredLogs`.

**Rationale**: The existing module already owns the recent REST backfill, SignalR live stream, source selection, bounded row list, filters, and dense log-scanning controls. The requested product shift is identity and structured-log semantics, not a parallel feature.

**Alternatives considered**:

- Keep `ServerLogs` and create a new module later: rejected because the current module would keep communicating the wrong console/raw-stream concept.
- Add compatibility shims for old public types: rejected for active code because the branch is unpublished feature work and consistent breaking renames are clearer.

## Decision: Use diagnostics structured paths and feature gate

**Decision**: Gate on `Elsa.Diagnostics.StructuredLogs`, call `/diagnostics/structured-logs/recent` and `/diagnostics/structured-logs/sources`, and connect to `/elsa/hubs/diagnostics/structured-logs`.

**Rationale**: The namespace umbrella should match the backend feature. A canonical diagnostics path also leaves `/diagnostics/console-logs` and OpenTelemetry paths available for future modules.

**Alternatives considered**:

- Keep `/server-logs` endpoints under a renamed UI: rejected because paths remain part of the active module identity and would continue the old concept.
- Probe both old and new remote feature names: rejected for the primary implementation because the branch coordinates with the paired Core rename. A friendly unavailable state is sufficient for partial rollouts.

## Decision: Add a Diagnostics menu group in Studio

**Decision**: Add `MenuItemGroups.Diagnostics = new("diagnostics", "Diagnostics", ...)` and put Structured Logs there.

**Rationale**: The current menu model only has General and Settings, while the spec explicitly wants Structured Logs under Diagnostics if supported. Adding a group is a small shared framework change and avoids burying diagnostics tools among general workbench items.

**Alternatives considered**:

- Keep General: acceptable fallback if grouping were impossible, but the framework already centralizes group definitions.

## Decision: Preserve dense scanner controls

**Decision**: Keep pause/resume, clear, reconnect, copy selected/visible, auto-scroll, wrap, compact mode, source selector, row cap, and dropped event indicators.

**Rationale**: Operators still need fast scanning during incidents. The UI should stop looking like a raw terminal, but the operational controls remain useful for structured records.

**Alternatives considered**:

- Replace the list with a card-only detail browser: rejected because it reduces scanning density and conflicts with the constitution's operational workbench guidance.

## Decision: Add a row details panel for semantic inspection

**Decision**: Selecting/opening a row shows a details panel with rendered message, template, event ID/name, properties, scopes, exception, source metadata, trace/span IDs, workflow IDs, tenant, correlation, and raw JSON/copy output.

**Rationale**: Structured logs are valuable because users can inspect semantic fields separately from rendered text. A details panel lets rows stay dense while exposing full payloads.

**Alternatives considered**:

- Inline expand every row: rejected because large properties and stack traces can destroy scan density.

## Decision: Extend filter state rather than introducing a new query abstraction

**Decision**: Rename and extend the existing filter model/mapper to include span ID and exact levels while preserving current URL query mapping patterns.

**Rationale**: The current mapper already protects request row caps and copies mutable collections. Extending it is simpler and DRY.

**Alternatives considered**:

- Build a separate URL-state service: deferred until more diagnostics modules share URL filter behavior.
