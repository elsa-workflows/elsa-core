# Elsa Diagnostics OpenTelemetry

Provides the planned OpenTelemetry diagnostics collector surface for Elsa hosts.

This module owns the Core-side OTEL contracts, bounded in-memory diagnostics store, feature registration, permissions, and collector metadata that Studio will consume. The first implementation slice establishes the module shape and normalized DTO contracts; OTLP protobuf ingestion, query endpoints, and SignalR streaming are implemented by the user-story tasks that follow.

The historical `Elsa.OpenTelemetry` module from `elsa-extensions` is intentionally not ported into this feature. It is producer-side workflow tracing middleware, duplicates current `Elsa.Workflows` instrumentation, and mutates `Activity.Current`; this module is collector/read-side diagnostics infrastructure.
