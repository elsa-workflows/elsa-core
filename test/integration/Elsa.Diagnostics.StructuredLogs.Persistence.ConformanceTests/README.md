# StructuredLogs persistence conformance suite

Shared InMemory / Sqlite assertions for `IStructuredLogStore`. The matrix locks
the contracts already aligned on `main` after #8147 (Take clamp) and #8148
(SourceId tie-break + registry-backed ListSources).

## Shared contracts

| Surface | Locked behavior |
| --- | --- |
| Null `Take` / max clamp | `StructuredLogsOptions.MaxRecentLogQuerySize`; negative ceiling or `Take` yields empty |
| Multi-source timestamp ties | `Timestamp`, `ReceivedAt`, `SourceId`, `Sequence`, `Id` (ascending after Relational DESC+Reverse) |
| `ListSources` | In-process `IStructuredLogSourceRegistry` metadata and `SourceHeartbeatTimeout` → `Stale` |
| `QueryAsync.DroppedEvents` when writes fit | `0` |
| Portable filters | Exact-case equality, category prefix, text substring, level, and timestamp range applied before `Take` |

## Store-specific contracts (intentionally not unified)

**`QueryAsync.DroppedEvents` after ring overflow.** InMemory reports
`RingBuffer.DroppedCount`. Relational QueryAsync stays `0`; durable write-queue
drops are `IStructuredLogStorageDiagnostics.DroppedWriteCount` on `/storage`.
The matrix asserts both sides of that split so neither path can silently adopt
the other.

**Filter equality case folding.** InMemory uses `OrdinalIgnoreCase`. Relational
SQL uses `=` (collation-dependent; Sqlite default is BINARY). Category `LIKE`
and text `LIKE` are ASCII case-insensitive on Sqlite, which happens to match
InMemory for those predicates only. Tests seed exact-case filter values.
Case-differing `SourceId` / `TenantId` / id equality is **not** portable across
providers — recreate filters after a store change rather than relying on
case-insensitive `=`. Same spirit as User Tasks title-cursor collation notes.

**SourceId sort case folding.** InMemory `ThenBy` uses `OrdinalIgnoreCase`.
Relational `ORDER BY SourceId` follows the column collation. Tie-break tests
use lowercase source IDs (`pod-a`, `pod-b`) so both providers agree.
