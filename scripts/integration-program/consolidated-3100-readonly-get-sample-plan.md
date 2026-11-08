# Report-only 3.10.0 read-only GET sample plan

**No measurement in this document.** No GET is issued here. This is a later-review
plan for a bounded, anonymous, read-only sample of the **staged 3.10.0** feed
identity. It is **not** PUT latency, ingestion, throttling, duplicate-success,
snupkg-archive, or end-to-end 3.10.1 fit evidence.

Before any sample **write** (executor/workflow that performs even GET against
the live feed as a recorded run), require reviewed invariants, the 3.10.1
operation DAG, QA, and a FINAL hash owner signoff. Report gaps rather than
claiming the sample is safe.

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

Use the pinned 3.10.0 original.zip / pre-upload manifest as the **local**
stratification source when a reviewed fixture is available. This repository's
pin inventory lists 231 IDs and does **not** carry per-package byte sizes or
symbol keys.

## Credentials and transport

- Anonymous / intended **reader** only. No `ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY`.
- No PUT capability in the sample client. GET only.
- URL allowlist: feed index, PackageBaseAddress nupkg URLs, symbol-store keys
  under the symbols origin. Reject `..`, redirects, retries, and other hosts.
- No GitHub metadata token in the sample (admission is out of scope).

## Stratification (from local 3.10.0 manifest once a fixture exists)

Select nupkg and PDB targets covering:

| Stratum | Nupkg | PDB / symbol key |
| --- | --- | --- |
| small | min size | min PDB bytes |
| typical | p50 | p50 |
| large | p95 | p95 |
| max | max | max |
| TFM | one package with net8/net9/net10 assemblies if present | one key per TFM of that package |
| key overlap | packages sharing a PDB **name** | same Feedz key if inspector shows overlap; else record uncovered |
| no overlap | unique-key control | unique-key control |

**Uncovered until a reviewed 3.10.0 original.zip is measured locally (still
without network):** exact byte sizes, Feedz keys (GUID/stamp), true key
overlaps vs same-name different-bytes TFMs, snupkg archive GET (unsupported
contract), GitHub metadata paging, intended-reader 401/403 vs anonymous 404.

If a stratum cannot be filled from the manifest, leave it uncovered. Do not
substitute 3.10.1 fixture sizes as 3.10.0 sample identity.

## Request budget (not yet executed)

- Choose N first-time targets (stratified nupkg and/or PDB URLs).
- Repeat each target once (first + repeat).
- Enumerate required index GETs (feed index; version lists only if allowlisted
  and required to form PackageBaseAddress URLs).
- `R = 2N + enumerated_indexes`. Require `R <= 5000`.
- Per-response cap: package `MAX_BYTES` (32 MiB); index 1 MiB.
- Aggregate cap: sum of intended Content-Length / actual bytes; stop if the
  remaining parent deadline cannot cover cap × remaining work + receipt reserve.

## Time and concurrency (not yet executed)

- One parent deadline: 1800s from inspector/sample start, including receipt
  reserve, early stop, reaping, and partial durable receipt.
- Per-request cap 25s.
- Fixed 4-exchange semaphore. Recheck the parent deadline after queue wait.
- Deadline-aware locks if any key is re-read. No `ceil(work/4)` fit claim.
- Early stop on redirect, oversized response, deadline, or request budget.
- Reap in-flight work on parent deadline. Keep a partial durable receipt.

## Observations to record (raw; no fake p99)

For each attempt: source identity, runner, request URL hash (not credentials),
HTTP status, complete flag, failure category, bytes, queue wait, startup,
transfer, body SHA-256, wall time. DNS/TLS timings **if observable** from the
bounded transport; otherwise record `unobservable`. Store raw samples. Do not
invent p99/p95 latency from empty or mock data.

## What this sample is not

- Not PUT latency or PUT success.
- Not ingestion, throttling, or duplicate-success classification.
- Not original snupkg archive proof (`remote_snupkg_archive_verified` stays
  false unless a supported archive GET is actually demonstrated).
- Not end-to-end 3.10.1 Feedz publish fit against 1800s.

## Gaps (do not paper over)

- No live GET has been run for this plan.
- 3.10.0 original.zip sizes/keys are not in this PR.
- Anonymous vs intended-reader behavior is unknown.
- Whether PackageBaseAddress already holds 3.10.0 bytes is unknown and is
  **not** a 3.10.1 GO input.
- Owner signoff of FINAL packet hash is still pending on the 3.10.1 identity
  track (#9199).
