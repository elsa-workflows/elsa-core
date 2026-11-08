# TEMPLATE: report-only 3.10.0 read-only GET sample plan

Owning issue: https://github.com/elsa-workflows/elsa-core/issues/8220
Removal condition: delete when a reviewed 3.10.1 executor supersedes this
prototype or the 3.10.1 Feedz path is abandoned.

**Status: TEMPLATE, not executable.** No measurement in this document. No GET
is issued here. A later reviewed run would still need pinned local 3.10.0
manifest-derived exact targets before it could execute.

This is **not** PUT latency, ingestion, throttling, duplicate-success,
snupkg-archive, or end-to-end 3.10.1 fit evidence.

## Fixture gap (blocking for execution)

A pinned **local** 3.10.0 `original.zip` / pre-upload manifest is **not** in
this workspace (`3.10.0 original.zip is not in this workspace`). Pin inventory
lists 231 IDs only (no sizes, hashes of nupkg bodies, or symbol keys).

**Do not** substitute the 3.10.1 local fixture. **Do not** fetch the 3.10.0
artifact from the network for this task.

Until that local 3.10.0 fixture exists, these execution inputs stay **UNKNOWN**:

| Input | Value |
| --- | --- |
| Exact N (first-time targets) | UNKNOWN |
| Exact target URLs | UNKNOWN |
| Exact nupkg SHA-256 list | UNKNOWN |
| Exact PDB / Feedz keys | UNKNOWN |
| Exact sizes (small/typical/large/max) | UNKNOWN |
| Enumerated index count I | UNKNOWN |
| R = 2N + I | UNKNOWN (must be ≤ 5000 once known) |
| Numeric per-response ceilings | UNKNOWN (template: nupkg 32 MiB, index 1 MiB — not pinned to selected targets) |
| Numeric aggregate byte ceiling | UNKNOWN |
| Cleanup seconds in the sample parent | UNKNOWN (template mentions 1800s parent + 25s cap + 4 semaphore; not a committed sample budget) |

## Identity in scope (3.10.0, not 3.10.1)

| Field | Value |
| --- | --- |
| Version | `3.10.0` |
| Source | `5cff67228e550b353b3483eb3b37dfdb73a41c74` |
| Run / artifact | `40347931480` / `12664519771` |
| Archive SHA-256 | `2dd253e1f2946ddcb2b8e75ef397596001757ddbcb0a91f717e9505bbf28b4f6` |
| Manifest SHA-256 | `b6a1750f1f796f68a441bef93f67386b4a862dbca94d498ecf64536b102c2994` |
| Feed index | `https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json` |
| Package base | `https://f.feedz.io/elsa-workflows/elsa-3/nuget/v3/packages` |
| Symbols | `https://f.feedz.io/elsa-workflows/elsa-3/symbols` |

## Credentials and transport (when a future executable plan is written)

- Anonymous / intended **reader** only. No publisher key.
- No PUT capability. GET only. URL allowlist. No redirect. No retry.
- Runner / client provenance and HTTP cache caveats must be recorded (cache
  hits are not origin latency). Those fields are UNKNOWN until a runner exists.

## Stratification (cannot be filled without the local 3.10.0 fixture)

small / typical / large / max nupkg and PDB, TFM, key overlap, no-overlap
control. Uncovered strata stay uncovered. Do not invent members.

## Required shape of a later executable plan (not this file)

- Pin exact targets from the local 3.10.0 manifest.
- Commit `R = 2N + I` with concrete N and I, `R <= 5000`.
- Precommit numeric per-response and aggregate byte ceilings for those targets.
- One 1800s parent including cleanup, deterministic early stop, reaping, partial
  receipt.
- Record source/runner/hash/status/bytes/queue/startup/transfer/hash/wall and
  DNS/TLS only if observable. No fake p99.

## What this template is not

- Not PUT latency or PUT success.
- Not ingestion, throttling, or duplicate-success classification.
- Not original snupkg archive proof.
- Not end-to-end 3.10.1 Feedz publish fit against 1800s.
- Not an authorization to execute.

Writes still require reviewed invariants, the baseline formula (DAG incomplete),
QA, and FINAL hash owner signoff.
