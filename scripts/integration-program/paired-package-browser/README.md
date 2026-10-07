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

The hosted workflow installs .NET SDK 10.0.300 with the .NET 8 and 9 runtimes and
Node 22.23.3. Both hosted and local execution select the sole reviewed SDK from
the converter policy using a private `global.json` with roll-forward disabled,
verify the actual SDK, and pin it in each disposable host group. A newer ambient
SDK cannot silently replace the reviewed converter SDK. Client projects also pin
the reviewed WebAssembly pack before framework-reference resolution so SDK
defaults cannot select a different pack version. Actual trace, archive, and loaded
assembly checks remain mandatory. Execution receipts record
the last started build operation using fixed component and phase names; these
locate failures without retaining exception text, command arguments, or logs.
Use fresh output and candidate-extraction paths
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
The materializer restores and builds that client first, then uses
`--no-dependencies` for the wrapper's restore and build. This preserves the
client's project-local NuGet configuration identity and verified build output;
it does not relax the source-edge or package provenance checks. See the .NET
[restore](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore)
and [build](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build)
option documentation.

Inherited client assets in the wrapper's static manifest have separate fixture
authority: approved project/input bytes, matching client manifest metadata and
exact completed-build hashes. The client's generated scoped CSS bundle is pinned
when the standalone client first builds, including explicit absence when the SDK
has no scoped CSS inputs. Hosted reuse and later phases require the same state;
they cannot force a second converter capture over existing WebCIL output. This
fixture authority does not replace any mandatory package asset or managed-byte proof.

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
- `cells/3.10.0-FRAMEWORK-HOST/react-phase.json` for each candidate host,
  bound to the exact unchanged X6 browser receipt;
- `cells/VERSION-FRAMEWORK-HOST/released-document.json` only for a passing
  3.8.4 or 3.9.0 source cell, after strict synthetic-content validation and
  binding to its fixture, browser, package, runtime and resource evidence.

The original browser receipt is preserved unchanged. The combined matrix
result adds independently verified package/resource, React-phase and released-export assertions after
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

Candidate full-shell JSON proof uses the native editor Export download and Import
file chooser, then native save/reload and the same candidate's publish/run/output
checks. Semantic hashes retain definition/root/activity identities, tenant scope,
expressions, inputs, outputs, options, custom properties and other model fields;
only API navigation links and known export/version metadata are excluded. The DOM proof requires the native
Import action to open its real file chooser and complete import/save callbacks.
Receipts contain hashes and bounded checks, never raw candidate documents. These
protocols still require actual browser verification. The CustomElements adapter
uses the same native editor actions through registered definition-list, editor,
instance-list and viewer elements. Its separate embedding receipt binds native
authentication and callback identities to the workflow read from the backend.
The nonpackable consumer host adds JavaScript-compatible EventCallbacks while
preserving its existing .NET Func parameters; the fixture records this host-glue
transformation explicitly. This is not evidence that the unchanged pinned host
already exposed those events. Package and archive bytes remain original.

Candidate execution keeps one backend alive while running X6 and then
React in separate Studio processes and fresh browser contexts. The second phase
requires X6 authentication, identity, edit/save/reload and cleanup evidence first.
It finds the same synthetic workflow through the native definitions list, selects
the activity in React, changes its literal through the native property editor,
saves and reloads it, and confirms the original definition/root/activity identities.
Read-only backend observations verify the before/after values. The second browser
must request the exact original React bundle bytes. No HTTP write substitutes for
the native editor callbacks.

Optional-feature probes use separate empty private runtime roots with fresh
databases, credentials and keys, while reusing the verified build. Their process
owner provides a one-shot backend disconnect bound to captured process objects
and birth identities. It verifies Studio remains alive after the stop and shares
the watchdog/cleanup lock; an expired owner, early process exit or uncertain
cleanup cannot establish a successful disconnect. Browser readiness and the
post-stop native action still need independent evidence from the probe transport.
The lifecycle contracts alone do not supply that browser evidence.

The optional browser phase uses a private inherited Unix socket for its ordered
action boundary and disconnect acknowledgement. The parent finishes its bounded
observation or owned stop before acknowledging; the child then performs the native
action. Credentials remain on stdin, and socket messages are never retained as
portable proof. The Node loader runs in the process that inherits the descriptor.
Socket, parser and transport contracts do not establish feature acceptance; the
parent must bind the resulting native observations and verify final cleanup.
Browser-native traffic records action and disconnect boundaries when each matching
request starts. A delayed response or failure retains those original flags; its
completion time cannot turn pre-action traffic into proof of a later native action.

Each of the twelve candidate cells must complete all five separate profiles:
without Secrets, without WorkflowContexts, denied Secrets, denied WorkflowContexts,
and a parent-owned backend disconnect. Thus full coverage requires sixty optional
observations in addition to the original 36-cell matrix. The parent retains each
profile's actual readiness, endpoint owners, browser observation and cleanup in
`execution.json`; the upload guard recomputes acceptance. A complete observation
of a defect stays failed. Uncertain cleanup prevents the next runtime from starting.
Server snapshots bind request-entry cursors to canonical package handlers and
passively measured response bodies; disconnect uses only its pre-stop snapshot.

`react-phase.json` holds bounded checks, hashes and its own resource observations.
The combined matrix verifies both phases' resources against the same inventory;
`browser.json` stays unchanged. The upload guard binds the exact bytes of both
receipts to execution and matrix summaries, including the original workflow
identities and the changed value. Partial failed phases may be retained but cannot
pass the matrix. These protocol checks do not substitute for actual browser runs;
CustomElements uses the native embedding consumer in the second phase and requires
complete original embedding callbacks and instance-viewer evidence before starting.
Its React activity callback must identify the same X6 activity. Actual composed
browser coverage for all hosts remains required and pending.

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

Standalone `wasm_boot` recognizes the observed SDK 10.0.300 /
net10.0 embedded configuration format and source-derived net8/net9 JSON formats.
The latter require the selected WebAssembly task package 10.0.8; actual net8/net9
build manifests and browser callbacks remain pending verification. Net10 binds five platform bootstrap assets
(the Blazor loader, configuration loader, native/runtime JavaScript and native
WASM) to the owned static manifest and matching source copies. This platform
source authority is distinct from sealed Elsa package provenance. Every original
Elsa/fixture managed resource remains bound to its existing verified inventory;
the managed count is derived, never assumed. Passing requires observed response
bytes and content types, matching embedded configuration, and an executed native
managed form-validation callback. Parsing a manifest or downloading assemblies
alone cannot pass. Net8/net9 bind the separate JSON configuration and loader as
six platform assets. Unknown formats fail closed; this bounded proof does not certify HostedWASM, CustomElements,
ICU/globalization or all platform assemblies.

On browser process timeout, the runner identifies and freezes the live launch
root and its descendants, including descendants in separate sessions. Signals
are guarded by process birth identity; TERM and KILL waits are bounded. Missing
ownership or uncertain termination raises `BrowserCleanupUnverified`, and host
cleanup cannot overwrite that uncertainty with a successful combined receipt.
Receipts retain only bounded cleanup failure categories, never raw process errors.
On macOS, child discovery uses bounded native PID inventories and confirms stopped
parents before traversal. Its inventory deadline is cooperative and cannot
interrupt a native call already blocked in the kernel.
Historical descendants already reparented before ownership discovery cannot be
safely recovered by this mechanism; it fails closed without guessing by process
name. The helper supports macOS and Linux process identities; live lifecycle
contracts must run on the actual target platform.

### Imported module pair coverage

This reconciliation describes this fixture and its retained evidence. Package
provenance and consumer builds establish package availability; they do not prove
that every registered Studio module works with its backend. The candidate source
remains `ba5b348aa2414fdf7c19d9d8806e87b7c91a4205` throughout these checks.

| Surface | Composition and evidence boundary |
| --- | --- |
| Identity, shell and Workflows | The backend enables local Elsa Identity, workflow management/runtime with SQLite, Workflows API and JavaScript. Native sign-in, registry and editor journeys are required by the matrix. Released cells do not certify candidate behavior; full candidate acceptance remains pending until every required record passes. Production identity providers are outside this fixture. |
| WorkflowContexts and Secrets | These are the representative optional pairs. Native metadata/reference journeys, canonical Secrets endpoint ownership, and separate absent, denied and disconnected probes are required. Implemented probes alone are not runtime proof, and these representatives do not certify every provider. |
| Weaver, Alterations, OpenTelemetry, Console Logs, Structured Logs and User Tasks | Common Studio host glue registers these modules, but this backend composition does not enable their corresponding features. Their runtime interactions are not certified here. Package/build visibility is separate evidence. |
| External Authentication | Studio management registration remains feature-gated; the fixture selects Elsa Identity and does not enable an external broker backend. It does not certify broker sign-in or management. |
| Environments, Labels and HTTP Webhooks | Environments registration is commented out, and Labels and HTTP Webhooks are not registered in this composition. This fixture supplies no paired runtime proof for them. |
| Settings, Security, Localization and Translations | These Studio surfaces are registered. No independent Core module pairing is asserted merely from their names. Resource-byte verification, including localization assets, establishes delivery integrity rather than every UI behavior. |

Composition sources are [common Server registration](hosts/common/server/Program.cs)
and the [owned backend](backend/Program.cs). The [program inventory](../../../doc/integration-program/inventory/README.md)
defines pairing by API, protocol or package-graph dependence. The accepted package
consumer work in [#8635](https://github.com/elsa-workflows/elsa-core/issues/8635)
and bounded SQLite/default-tenant upgrade work in
[#8219](https://github.com/elsa-workflows/elsa-core/issues/8219)
remain separate from the paired browser acceptance tracked in
[#8643](https://github.com/elsa-workflows/elsa-core/issues/8643).

This fixture performs no package publication, deployment, publisher cutover or
repository archival. It does not certify mixed-version pairs, npm distributions,
production identity providers or every optional connector/provider.
