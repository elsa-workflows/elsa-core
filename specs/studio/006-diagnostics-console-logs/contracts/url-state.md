# Contract: URL State

## Route

```text
/diagnostics/console
```

## Canonical Query Parameters

| Parameter | Values | Default | Notes |
|-----------|--------|---------|-------|
| `source` | Stable backend `source.id` | omitted means `All sources` | Unknown values may remain selected as stale/disconnected. |
| `stream` | `stdout`, `stderr`, or `both` | `both` | Invalid values fall back to `both`. |
| `text` | URL-encoded string | omitted | Sent to recent and live backend filters and used for highlight. |
| `from` | ISO-8601 UTC timestamp | omitted | Inclusive start time. |
| `to` | ISO-8601 UTC timestamp | omitted | Inclusive end time. |
| `wrap` | `true` or `false` | implementation default | Controls viewer wrapping. |
| `compact` | `true` or `false` | implementation default | Controls row density. |
| `ansi` | `true` or `false` | implementation default | Controls raw ANSI sequence display versus stripped plain text. |
| `follow` | `true` or `false` | implementation default | Controls follow-tail behavior. |

## Validation

- Parse `from` and `to` as ISO-8601 UTC timestamps.
- Reject invalid boolean values and fall back to safe defaults.
- Reject invalid `stream` values and fall back to `both`.
- Preserve encoded spaces, quotes, punctuation, and other text-filter characters.
- When `source` does not match the current source list, keep it visible as stale/disconnected if it came from the URL or current rows.
- Apply validated URL state before recent loading and live subscription start.

## Shareability

Refreshing or sharing a URL with source, stream, text, time, wrap, compact, raw ANSI, and follow-tail state restores those settings without a full page reload beyond normal route activation.
