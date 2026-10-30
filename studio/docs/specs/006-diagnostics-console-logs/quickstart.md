# Quickstart: Diagnostics Console Logs Studio Module

## Prerequisites

- Use the `006-diagnostics-console-logs` branch.
- Work only in `elsa-studio`; do not modify `elsa-core`.
- Use a backend or mock host that advertises the diagnostics console logs remote feature and read permission.
- Backend contract expected by Studio:
  - `POST /diagnostics/console-logs/recent`
  - `GET /diagnostics/console-logs/sources`
  - SignalR hub resolved from `hubs/diagnostics/console-logs`; with the default `/elsa/api` backend base this is `/elsa/hubs/diagnostics/console-logs`

## Build

```bash
dotnet build Elsa.Studio.sln
```

Latest result: Passed on 2026-05-18 with 0 errors and existing solution warnings.

## Targeted Tests

When implementation tasks exist, run the ConsoleLogs tests directly:

```bash
dotnet test src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/Elsa.Studio.Diagnostics.ConsoleLogs.Tests.csproj
```

Latest result: Passed on 2026-05-18 with 22 tests.

Suggested first test coverage:

- `ConsoleLogFilterMapper` maps URL/default filters to recent and live backend filters.
- Invalid URL state falls back to safe defaults.
- `source.id` is used for source filters while display metadata remains label-only.
- `text` is sent to recent and live filters and remains highlighted in visible rows.
- Copy/export formatting includes timestamp, stream, source, and raw text.

## Manual Verification

1. Start Studio against an enabled backend or mock diagnostics console service.
2. Open `/diagnostics/console`.
3. Verify recent stdout/stderr lines appear before live updates begin.
4. Verify stderr is visually distinct and every row shows timestamp, stream, source identity, and raw text.
5. Toggle pause/resume, follow-tail, clear local view, reconnect, wrap, compact, and raw ANSI display.
6. Apply source, stream, text, `from`, and `to` filters and confirm recent and live data use the same filter.
7. Refresh or share a URL containing `source`, `stream`, `text`, `from`, `to`, `wrap`, `compact`, `ansi`, and `follow`; confirm state is restored.
8. Simulate missing backend feature and missing permission; confirm navigation is hidden and direct route shows unavailable or unauthorized state.
9. Simulate live disconnect/reconnect and backend dropped-line/truncated-line summaries.
10. Load at least 10,000 local lines and confirm the row cap prunes older rows while reporting discarded local rows.

Manual verification status: Not run in this planning/implementation pass because no live diagnostics console backend was started.

## Distinction From Structured Logs

Console rows are raw stdout/stderr text. The Console module must not render rows as structured log records and must not expose structured fields such as level, category, message template, properties, scopes, trace ID, or span ID.
