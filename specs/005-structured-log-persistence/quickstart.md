# Quickstart: Structured Log Persistence

## Default in-memory storage

No durable storage configuration is required:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseStructuredLogs(options =>
    {
        options.RecentLogCapacity = 5_000;
        options.MaxRecentLogQuerySize = 1_000;
    });
});
```

This keeps the current bounded in-memory recent history and live SignalR streaming behavior.

## SQLite durable storage

Add the SQLite persistence package and configure a database file:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseStructuredLogs(structuredLogs =>
    {
        structuredLogs.UseSqliteStorage("Data Source=elsa-structured-logs.db", sqlite =>
        {
            sqlite.RunMigrationsOnStartup = true;
            sqlite.WriteQueueCapacity = 10_000;
        });
    });
});
```

Then map the existing structured logs endpoints and hub:

```csharp
app.UseStructuredLogs();
```

Studio continues to use:

- `/diagnostics/structured-logs/recent`
- `/diagnostics/structured-logs/sources`
- `/elsa/hubs/diagnostics/structured-logs`

## Migrations

SQLite storage runs FluentMigrator migrations when `RunMigrationsOnStartup` is enabled. For SQLite this is the recommended default.

For future shared relational providers such as SQL Server or PostgreSQL, production deployments may choose to run migrations once during deployment instead of from every application instance. Multi-instance startup locking must be documented by each provider.

## Retention

SQLite storage does not delete persisted log entries by default. Configure `Retention.MaxAge`, `Retention.MaxRows`, or both to bound durable storage growth:

```csharp
sqlite.Retention.MaxAge = TimeSpan.FromDays(14);
sqlite.Retention.MaxRows = 250_000;
```

## Write buffering

SQLite writes are batched through a bounded background queue. Graceful shutdown flushes queued events where possible. If the process crashes, queued-but-unflushed events can be lost. If the queue is full, newly received events are dropped and dropped-write counts are reported.

## Validation

Run targeted checks after implementation:

```bash
dotnet build src/modules/Elsa.Diagnostics.StructuredLogs/Elsa.Diagnostics.StructuredLogs.csproj
dotnet build src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.csproj
dotnet test test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Elsa.Diagnostics.StructuredLogs.UnitTests.csproj
dotnet test test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests.csproj
```

Manual validation:

1. Start Elsa Server with SQLite structured log storage enabled.
2. Emit several `ILogger` records with different levels, categories, workflow IDs, and correlation IDs.
3. Query recent logs from Studio and verify filters work.
4. Restart the host using the same SQLite database file.
5. Query recent logs again and verify events from before the restart are still available.
6. Verify timestamps are stored and filtered as UTC ISO-8601 values.
7. Lower retention settings in a test environment and verify cleanup removes old or excess rows.
8. Saturate the write queue in a test environment and verify newest events are dropped with visible dropped-write counts.

## Out of scope

This feature does not add OTLP, Logstash, Datadog, Splunk, Loki, Seq, or Parquet exporters. Those can be added later as sinks/exporters after persistence is stable.
