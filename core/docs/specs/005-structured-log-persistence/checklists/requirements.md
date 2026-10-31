# Requirements Checklist: Structured Log Persistence

- [x] Requirements preserve in-memory behavior as the default.
- [x] Requirements define SQLite as the only initial durable provider.
- [x] Requirements separate storage, live feed, and provider facade responsibilities.
- [x] Requirements keep redaction before all storage and live delivery.
- [x] Requirements avoid EF Core and specify FluentMigrator for schema management.
- [x] Requirements make future relational providers possible without changing Studio contracts.
- [x] Requirements include retention and graceful shutdown flushing.
- [x] Requirements explicitly defer OpenTelemetry and vendor exporter scope.
