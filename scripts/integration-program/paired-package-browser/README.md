# Paired package browser proof

This fixture verifies aligned server and Studio NuGet packages for program task
[#8643](https://github.com/elsa-workflows/elsa-core/issues/8643). It accounts for
36 cells: Server, standalone WASM, hosted WASM and CustomElements; net8.0,
net9.0 and net10.0; released 3.8.4/3.9.0 and the original unpublished 3.10.0
candidate. The coverage policy is in
[`coverage-policy.json`](../../../test/studio/browser/PackageCompatibility/coverage-policy.json).

The implementation is still in progress. A successful focused browser journey
does not accept a cell or the matrix. Missing journeys and unreviewed WASM
converter evidence fail closed. Keep the issue's required outcomes open until
the complete ledger, independent review and exact-head CI have passed.

## Inputs and execution

Use a clean isolated worktree. The candidate input directory must contain only
`candidate.zip`, `artifact.json`, `producer-run.json` and `live-retrieval.json`.
These are a fresh retrieval of original artifact `11412848210` from run
`37456860080`, attempt 1. The verifier checks the pinned producer, archive,
manifest, original envelope and expiry. Rebuilding packages or replacing these
files with a later regression artifact is not supported.

The hosted workflow separates token-bearing retrieval from checkout execution.
The proof job receives the original inputs through a same-run artifact and has
no Actions read permission. Do not pass artifact-access credentials into the
local verifier or browser process.

Install the locked browser tooling from the repository root:

```sh
npm --prefix test/studio/browser/PackageCompatibility ci --ignore-scripts --no-audit --no-fund
npm --prefix test/studio/browser/PackageCompatibility run typecheck
npm exec --prefix test/studio/browser/PackageCompatibility --no -- playwright install chromium
```

The hosted workflow selects .NET SDK 10.0.300 with the .NET 8 and 9 runtimes and
Node 22.23.3. Local execution records the selected .NET 10 SDK and pins it in
each disposable host group. Use fresh output and candidate-extraction paths
outside the checkout:

```sh
python3 scripts/integration-program/run_paired_package_browser_matrix.py \
  --inputs /absolute/path/to/original-inputs \
  --candidate-artifacts /absolute/path/to/new-verified-candidate \
  --output /absolute/path/to/new-proof-output \
  --fixture-source "$(git rev-parse HEAD)"
```

`--fixture-run` and `--fixture-attempt` are supplied together by CI. A local
diagnostic can add `--cell 3.10.0/net10.0/server`; it remains development-only
and returns a nonzero exit status even when that selected journey succeeds.
Do not use a selected cell for hosted acceptance.

Restores use isolated version/framework caches. All internal package versions,
sources and archive hashes must match the original candidate or the reviewed
release source policy. Nonpackable fixture executables are built from recorded
host glue; Elsa libraries cannot fall back to source project references. The
hosted WASM wrapper has one explicit nonpackable client-fixture edge.

## Reading evidence

`retained-evidence/matrix.json` always accounts for all 36 identities. Overall
acceptance requires `complete_matrix: true`, `passed: true`,
`development_only: false`, and every required assertion in every cell passing.
`not_run`, incomplete and failed cells cannot be accepted. If an early cell
fails, later cells remain explicitly `not_run`.

The retained directory permits only:

- `inputs/original-envelope.json`, `inputs/live-retrieval.json` and
  `inputs/provenance.json`;
- `matrix.json`;
- `cells/VERSION-FRAMEWORK-HOST/execution.json` and `browser.json` for known
  matrix identities;
- `cells/VERSION-FRAMEWORK-HOST/released-document.json` only for a passing
  3.8.4 or 3.9.0 source cell, after strict synthetic-content validation and
  binding to its fixture, browser, package, runtime and resource evidence.

The original browser receipt is preserved unchanged. The combined matrix
result adds independently verified package/resource and released-export assertions after
owned-process cleanup; it cannot promote a failed browser result. Per-cell
execution receipts record the stage and bounded failure category when execution
stops. The pre-upload inventory guard rejects unexpected files, directories,
links and non-object JSON payloads.

Released documents retain the exact native download bytes. The inventory guard
revalidates their shape and all source bindings before upload. A failed source
cell leaves its download private. Studio 3.8.4 writes `toolVersion: 3.8.0.0`;
this document marker is checked separately from installed package provenance.
Retaining a released document does not prove the candidate can reopen it.

Candidate cells require the completed 3.8.4 and 3.9.0 source cells for the same
host and framework. Their bytes and source bindings are checked again before
native definitions-list import. Each reopen proof records import, visible
activity/value, save, reload, publish, Studio terminal state and backend output.
Both journeys must finish before `baseline_reopen` can pass; partial proof stays
in the original browser receipt. A selected candidate-only development run
without those source cells fails before building or starting hosts.

Private build logs, caches, credentials, runtime data and browser state remain
under `private/` and are not uploaded. An inventory-approved file is not proof
that its assertions passed; inspect the matrix and per-cell outcomes. Producer
identity, fixture commit/run, package ownership and actual browser execution
remain separate evidence.

All six Designer/DomInterop assets must be materialized and match the original
package bytes, including the standalone `designer.css` file. Network requests
are a separate, host-specific check: the Server host requires the standalone
stylesheet request because its pinned host template links that file. The pinned
WASM, hosted-WASM and CustomElements host templates do not link it; their
designer bundle imports CSS through `style-loader`. Those hosts still require
the designer bundle and the other applicable package assets to be requested
with exact served bytes. Released baseline smoke continues to require the
designer entrypoint and DOM entrypoint on every host, plus the standalone
stylesheet request on Server. An optional standalone request never makes the
stylesheet optional as a package materialization or sealed-byte check.

WASM-managed resources additionally require reviewed conversion and
executed-byte evidence.

Non-Server execution prepares one isolated, locked converter decoder and captures
the actual client build's converter loads. Reused builds revalidate the original
traces. The selected approved tuple drives package PE-to-WebCIL verification;
static and managed resources share the browser response inventory without path
collisions. See [converter selection](converter-selection.md). Successful
conversion or resource delivery alone does not grant the remaining WASM host or
workflow assertions.

Standalone WASM records the native page's login and activity-registry responses
separately from the harness's backend API calls. Its `direct_backend` assertion
requires distinct loopback origins, exact CORS origin responses, the controlled
login, the matching bearer token on the later registry request, and the expected
activity identities. Only bounded statuses, checks, count and a response-body
hash enter receipts. Tokens, credentials and raw response bodies stay private.
This observation does not grant `wasm_boot` or either other WASM host's assertions.

On browser process timeout, the runner identifies and freezes the live launch
root and its descendants, including descendants in separate sessions. Signals
are guarded by process birth identity; TERM and KILL waits are bounded. Missing
ownership or uncertain termination raises `BrowserCleanupUnverified`, and host
cleanup cannot overwrite that uncertainty with a successful combined receipt.
Historical descendants already reparented before ownership discovery cannot be
safely recovered by this mechanism; it fails closed without guessing by process
name. The helper supports macOS and Linux process identities; live lifecycle
contracts must run on the actual target platform.

This fixture performs no package publication, deployment, publisher cutover or
repository archival. It does not certify mixed-version pairs, npm distributions,
production identity providers or every optional connector/provider.
