# Elsa Diagnostics Structured Logs Relational Persistence

This package contains the reusable relational storage layer for diagnostics structured logs. It is provider-neutral: concrete packages supply the connection factory, SQL dialect, and FluentMigrator runner wiring.

To add a new relational provider such as SQL Server or PostgreSQL:

1. Reference this package from the provider package.
2. Implement `IRelationalStructuredLogConnectionFactory`.
3. Implement `IRelationalStructuredLogDialect` for identifier quoting, parameter prefixing, and result limiting.
4. Implement `IStructuredLogSchemaMigrator` using FluentMigrator with the provider-specific runner.
5. Register those services and call `AddRelationalStructuredLogPersistence`.

The core `Elsa.Diagnostics.StructuredLogs` package must remain unaware of provider packages. Provider-specific SQL and migration runner dependencies belong in provider packages.
