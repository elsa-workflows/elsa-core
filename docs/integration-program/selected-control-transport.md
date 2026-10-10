# Selected control transport prerequisite

`selected_control_transport.py` is an offline byte-validation prerequisite for
Task #8693 hosted controls. It has **no seal, staging, extraction or command-line
entry point** and is not called by a producer, consumer or workflow. It cannot
admit a selected control or authorize an upload. The complete six-cell semantic
validator and hosted workflow remain required.

The module validates strict UTF-8 JSON, bounded original ZIP bytes, complete
manifest/file hash closure, safe casefold-unique paths and regular members. It
also inventories genuine-format NuGet ZIP and npm gzip/tar fixtures, keeps raw
archive SHA-512 distinct from npm SRI, and rejects hidden tar members after the
end marker. It never executes or extracts an archive. Explicit directory entries
are rejected; compatibility with actual selected outputs still requires the
reviewed vertical controls.

A transport-only manifest has exactly `schema`, `scope`, `context`, `controller`,
`product`, `line` and `files`. Its scope is
`transport-only-not-selected-control-acceptance`; a claim of control acceptance
fails validation. Each file row is exactly `path`, `size` and `sha256`. The
manifest excludes itself, and the expected external SHA-256 binds its original
bytes. Allowed transport path patterns are `plan.json`, `producer/receipt.json`,
`consumer/receipt.json`, `producer/nuget/*.nupkg`, `producer/nuget/*.snupkg`,
`producer/npm/*.tgz`, `producer/npm/receipt.json` and
`preupload-manifest.json`. These patterns are **not the plan-selected file
allowlist**: that exact allowlist is a still-required semantic check. Tests
intentionally demonstrate that transporting arbitrary receipt bytes is distinct
from admitting those receipts.

Hosted execution validation is pure and independent of the readback job's
`GITHUB_JOB` or environment. It preserves the original runner claim and checks
source/plan/controller/role/context equality, UUID identity, UTC start times and
distinct same-job artifact/consumer executions. It rejects local promotion.
It does not replace historical producer admission or the fresh consumer gate.

The closed provider projection contains `schema`, `repository`, `repository_id`,
`run_id`, `run_attempt`, `head_sha`, `head_branch`, `event`, `workflow_path`,
`control_job`, `control_conclusion`, `run_status`, `run_conclusion`, `artifact_id`,
`artifact_name`, `archive_sha256`, `archive_size`, `manifest_sha256`, `created_at`,
`expires_at`, `expired` and `retrieved_at`. It binds the reviewed repository,
workflow, trusted push ref, exact controller and upstream successful control.
A run may be in progress while readback executes; a completed run must have
succeeded. A future fixed retrieval job must derive this projection from actual
GitHub run/artifact responses and trusted upstream outputs. Accepting a projection
in this pure function does not prove it came from GitHub. No provider request has
been implemented or run. Availability is a retrieval snapshot; acceptance needs
a later current-provider recheck.

Resource ceilings are fixed: 2 GiB outer compressed bytes, 8 GiB outer expanded
bytes, 10,000 members, 512 MiB compressed and expanded per leaf package/tarball,
64 MiB per receipt/manifest and 32 MiB per plan. Exceeding a ceiling fails; nothing
truncates or automatically expands a budget. No successful product output or
provider artifact has been used to claim compatibility with these limits.

## Required next semantic work

The next bounded implementation should first close the Studio vertical payload,
then add Core and Extensions specializations, and only then expose the final
seal/stage/readback interface after every six-cell schema is supported. The
transport-only manifest is not a substitute for that final control manifest.

- Plan: close the concrete public plan/inventory/metadata/history/feed schemas;
  admit the original plan hash at actual producer start; bind exact controller
  input hashes, original source policies, selected identities, expected archive
  names, output/symbol policies and dependency/framework groups. Reuse the pure
  `prove_product_release_artifacts.admit` and exact source contracts, without
  process, environment, Git or network calls in independent readback.
- Producer: close the successful conditional writer, test counters and exact
  source-bound skip rows. Join every selected package file/inventory/hash/size to
  actual ZIP bytes and nuspec ID/version/repository/dependency/framework metadata.
  Explicitly account for `packages.private_recipe_only_outputs`: only excluded
  plan IDs, exact version and safe relative inventories/hashes/sizes are allowed;
  excluded archive bytes are never staged. Preserve original receipt bytes.
- Studio npm: the concrete historical writer has no embedded proof manifest or
  toolchain receipt. Reject invented fields. Close its actual outer source,
  execution, command and lifecycle-correction schemas; recompute both tarballs,
  package metadata hashes, names/versions, exact React-to-WASM dependency and SRI;
  join normal consumer local archive evidence and all three generated public
  asset families to WASM and wrapper bytes. Reuse the source-bound historical
  continuation contract; do not transplant the current Studio proof helper.
- Consumer: close historical and complete current admissions separately, including
  checked time, exact counts and observation digest. Validate complete selected
  package/TFM bijection, output-only accounting, restore/input/isolation ledgers,
  raw versus native content hashes and original feed evidence. Join every
  selected restored payload and runtime loaded asset to exact archive inventory
  bytes. Validate representative runtime source/API pins and all applicable TFMs.
- Core: close native per-asset assembly/PDB/symbol/document/satellite/SDK evidence,
  reuse `selected_core_consumer.bind_assemblies`, preserve the exact SamplePackage
  no-symbol exception and original conditional skip policies. Never derive Core
  assembly versions from the requested NuGet version.
- Extensions: close manifest/native SDK evidence, pin the generator/HTTP I/O
  representative policy and preserve original DLL `1.0.0+sourceSHA` separately
  from requested package version. Original manifest version conflicts remain
  failures. Native, compiler, SourceLink, runtime and test judgments are inherited
  from exact successful source-bound receipts, not rerun by Python.
- Final interface: build the exact conditional file allowlist and final control
  manifest only after all semantic checks; preserve canonical input bytes. On
  provider ZIP readback, repeat every semantic/hash/content join before any
  optional no-overwrite extraction. Implement fixed retrieval/workflow separately.

The accepted architecture remains same-job private planning snapshot, producer
and cold consumer execution, followed by independent original provider-ZIP
readback. This does not claim a consumer executed after retrieval. Raw planning
assets, logs, TRX, caches, source trees and private paths remain outside uploads.
