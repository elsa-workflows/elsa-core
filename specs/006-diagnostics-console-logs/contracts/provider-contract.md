# Provider Contract: Diagnostics Console Logs

Providers receive only redacted line text and redacted source metadata.

## IConsoleLogProvider

Responsibilities:

- Accept redacted `ConsoleLogLine` values.
- Return recent lines for a `ConsoleLogFilter`.
- Stream future lines and dropped summaries for a `ConsoleLogFilter`.
- List known redacted `ConsoleLogSource` values and source health.

```csharp
public interface IConsoleLogProvider
{
    ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default);

    ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
```

## IConsoleLogRedactor

```csharp
public interface IConsoleLogRedactor
{
    ConsoleLogLine Redact(ConsoleLogLine line);

    ConsoleLogSource Redact(ConsoleLogSource source);
}
```

## IConsoleLogSourceRegistry

```csharp
public interface IConsoleLogSourceRegistry
{
    event Action<ConsoleLogSource>? SourceChanged;

    ConsoleLogSource Current { get; }

    void MarkSeen(string sourceId, DateTimeOffset timestamp);

    IReadOnlyCollection<ConsoleLogSource> List();
}
```

## IConsoleLogCapture

```csharp
public interface IConsoleLogCapture : IAsyncDisposable
{
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
```

## In-Memory Provider

`InMemoryConsoleLogProvider` stores redacted lines in a bounded recent-history buffer and broadcasts live lines through bounded subscriber queues. It is the default provider for local development, tests, and single-node hosts.

## Provider Boundaries

- Providers must not receive raw unredacted console content.
- Providers must preserve deterministic ordering for overlapping source timestamps using received order or another stable tiebreaker.
- Provider failures return safe errors through endpoints or hub summaries without exposing unredacted content.
