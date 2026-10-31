# Elsa Diagnostics Structured Logs SQLite Persistence

This package persists diagnostics structured log events to SQLite using the shared relational structured log store.

## Configure

```csharp
services.AddElsa(elsa =>
{
    elsa.UseStructuredLogs(structuredLogs =>
    {
        structuredLogs.UseSqliteStorage("Data Source=elsa-structured-logs.db", sqlite =>
        {
            sqlite.RunMigrationsOnStartup = true;
            sqlite.Relational.WriteQueue.Capacity = 10_000;
            sqlite.Relational.WriteQueue.BatchSize = 100;
        });
    });
});
```

SQLite migrations run on startup by default. Set `RunMigrationsOnStartup` to `false` when migrations are handled by deployment tooling.

## Retention

SQLite storage does not delete persisted log entries by default. Configure retention explicitly:

```csharp
sqlite.Relational.Retention.MaxAge = TimeSpan.FromDays(14);
sqlite.Relational.Retention.MaxRows = 250_000;
sqlite.Relational.Retention.CleanupOnStartup = true;
```

## Write Queue

Writes are buffered through a bounded queue. Graceful shutdown flushes queued events where possible. If the queue is full, newest writes are dropped and `IStructuredLogWriteBuffer.DroppedWriteCount` reports the dropped-write count.

## Provider Boundary

SQLite-specific services live in this package: connection factory, SQL dialect, FluentMigrator runner wiring, and startup migration/cleanup service. Shared relational behavior lives in `Elsa.Diagnostics.StructuredLogs.Persistence.Relational`.
