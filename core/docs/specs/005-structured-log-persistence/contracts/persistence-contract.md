# Persistence Contract: Structured Log Persistence

## IStructuredLogSink

Append-only destination for redacted structured log events.

```csharp
public interface IStructuredLogSink
{
    ValueTask WriteAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default);

    ValueTask WriteManyAsync(IReadOnlyCollection<StructuredLogEvent> logEvents, CancellationToken cancellationToken = default);
}
```

## IStructuredLogStore

Queryable storage for redacted structured log events.

```csharp
public interface IStructuredLogStore : IStructuredLogSink
{
    ValueTask<RecentStructuredLogsResult> QueryAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
```

## IStructuredLogLiveFeed

Live subscription source for SignalR.

```csharp
public interface IStructuredLogLiveFeed
{
    ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default);

    IAsyncEnumerable<StructuredLogStreamItem> SubscribeAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);
}
```

## IRelationalStructuredLogConnectionFactory

Provider-owned connection creation.

```csharp
public interface IRelationalStructuredLogConnectionFactory
{
    ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
```

## IRelationalStructuredLogDialect

Provider-owned SQL differences.

```csharp
public interface IRelationalStructuredLogDialect
{
    string ProviderName { get; }

    string QuoteIdentifier(string identifier);

    string ApplyLimit(string sql, int take);
}
```

Additional methods can be added when implementation reveals provider-specific differences for timestamps, free-text filtering, or JSON handling.

## IStructuredLogSchemaMigrator

FluentMigrator-backed schema setup.

```csharp
public interface IStructuredLogSchemaMigrator
{
    ValueTask MigrateAsync(CancellationToken cancellationToken = default);
}
```

## IStructuredLogRetentionService

Durable cleanup boundary.

```csharp
public interface IStructuredLogRetentionService
{
    ValueTask CleanupAsync(CancellationToken cancellationToken = default);
}
```

## IStructuredLogWriteBuffer

Bounded async write queue used by durable stores.

```csharp
public interface IStructuredLogWriteBuffer : IAsyncDisposable
{
    long DroppedWriteCount { get; }

    ValueTask EnqueueAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
```

The SQLite implementation drops newly received events when the queue is full, increments `DroppedWriteCount`, and logs warning summaries. It does not block logging calls and does not allocate unbounded memory. Existing REST and SignalR code continues to use `IStructuredLogProvider` as the facade.

## Registration Sketch

Default in-memory:

```csharp
services.AddElsa(elsa => elsa.UseStructuredLogs());
```

SQLite:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseStructuredLogs(structuredLogs =>
    {
        structuredLogs.UseSqliteStorage("Data Source=elsa-structured-logs.db");
    });
});
```

SQLite migrations run on startup by default. Retention deletes no records unless `MaxAge`, `MaxRows`, or both are configured. The exact fluent API may change during implementation to match existing Elsa feature conventions.
