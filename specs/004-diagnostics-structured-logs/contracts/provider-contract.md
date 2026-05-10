# Provider Contract: Diagnostics Structured Logs

## IStructuredLogProvider

Responsibilities:

- Accept redacted `StructuredLogEvent` values.
- Return recent events for a `StructuredLogFilter`.
- Stream future events for a `StructuredLogFilter`.
- List known `StructuredLogSource` values.

```csharp
public interface IStructuredLogProvider
{
    ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default);

    ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);

    IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
```

## IStructuredLogStreamProvider

Extends `IStructuredLogProvider` with explicit dropped-event summaries for live streams.

```csharp
public interface IStructuredLogStreamProvider : IStructuredLogProvider
{
    IAsyncEnumerable<StructuredLogStreamItem> SubscribeWithDroppedEventsAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);
}
```

## IStructuredLogRedactor

```csharp
public interface IStructuredLogRedactor
{
    StructuredLogEvent Redact(StructuredLogEvent logEvent);
}
```

## IStructuredLogSourceRegistry

```csharp
public interface IStructuredLogSourceRegistry
{
    event Action<StructuredLogSource>? SourceChanged;

    StructuredLogSource Current { get; }

    void MarkSeen(string sourceId, DateTimeOffset timestamp);

    IReadOnlyCollection<StructuredLogSource> List();
}
```

## In-Memory Provider

`InMemoryStructuredLogProvider` stores redacted events in a bounded ring buffer and broadcasts live events through bounded subscriber queues. It is the default provider for development and single-node hosts.
