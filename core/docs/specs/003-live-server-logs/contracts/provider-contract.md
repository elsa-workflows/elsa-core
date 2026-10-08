# Contract: Server Log Provider

## IServerLogProvider

Responsibilities:

- Accept redacted `ServerLogEvent` values.
- Return recent events for a `ServerLogFilter`.
- Stream future events for a `ServerLogFilter`.
- List known `ServerLogSource` values.
- Report dropped events and source health.

Sketch:

```csharp
public interface IServerLogProvider
{
    ValueTask PublishAsync(ServerLogEvent logEvent, CancellationToken cancellationToken = default);
    ValueTask<RecentServerLogsResult> GetRecentAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ServerLogEvent> SubscribeAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
    ValueTask<IReadOnlyCollection<ServerLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
```

## Provider Expectations

- Providers MUST preserve source identity.
- Providers MUST enforce bounded memory or rely on bounded external storage/query limits.
- Providers SHOULD expose dropped-event metadata.
- Providers SHOULD expose source `LastSeen` or heartbeat data.
- Providers MUST NOT expose unredacted events.

## MVP Provider

`InMemoryServerLogProvider` stores redacted events in a bounded ring buffer and broadcasts live events through bounded channels.

## Future Providers

The contract must allow providers backed by Redis Streams, OpenTelemetry, Loki, Elasticsearch, Seq, Application Insights, or another observability store.
