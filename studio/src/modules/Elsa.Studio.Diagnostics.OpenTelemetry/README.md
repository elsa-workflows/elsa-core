# Elsa Studio Diagnostics OpenTelemetry

Adds the Studio-side OpenTelemetry diagnostics feature shell at `/diagnostics/opentelemetry`.

The module is a viewer for normalized Core diagnostics DTOs. Studio does not parse OTLP protobuf payloads and does not launch or configure external processes. Core remains the source of truth for ingestion, redaction, storage, permissions, and API contracts.

## Behavior

- Navigation is contributed under Diagnostics when the remote `Elsa.Diagnostics.OpenTelemetry` feature is available.
- The canonical route is `/diagnostics/opentelemetry`.
- API calls use `IOpenTelemetryApi` through the active backend API client.
- The module keeps OpenTelemetry separate from Structured Logs and Console Logs. Correlation uses public route/query contracts and trace/span/resource/time values.

## Expected States

The UI implementation should distinguish loading, empty, feature-unavailable, unauthorized, disconnected, reconnecting, error, and storage-overflow states. Live views must cap or prune visible items so telemetry growth cannot grow client memory without bounds.

## Contract Alignment

The mirrored DTOs in `Models/` intentionally track Core read contracts:

- resource search
- trace search and trace detail
- metric search
- OTLP log search
- storage diagnostics
- collector configuration

When Core contracts change, update this module's DTOs and `IOpenTelemetryApi` routes in the same change set.
