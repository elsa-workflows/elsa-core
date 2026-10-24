# Selected control transport prerequisite

`selected_control_transport.py` is an offline byte-validation prerequisite for
Task #8693 hosted controls. The helper itself has **no seal, staging, extraction
or command-line entry point**. Its byte/identity checks are used by the implemented
[semantic seal and readback](selected-control-seal.md) and the
[six-cell hosted workflow](../../.github/workflows/selected-product-release-control.yml).
Transport validation alone cannot admit a selected control or authorize an upload.
Actual complete six-cell native controls and hosted provider ZIP proof remain
unverified; source implementation does not establish that acceptance.

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
allowlist**: the implemented semantic validator derives that exact allowlist. Tests
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
succeeded. The workflow's implemented fixed retrieval job derives this projection
from GitHub run/artifact responses and trusted upstream outputs, validating all
six metadata tuples before downloading original ZIP bytes. Its API redirect is
handled explicitly; the token is not forwarded to object storage. Accepting a
projection in this pure helper does not prove it came from GitHub. Retrieval
implementation is separate from actual successful six-cell provider evidence.
Availability is a retrieval snapshot; acceptance needs a later current-provider
recheck.

Resource ceilings are fixed: 2 GiB outer compressed bytes, 8 GiB outer expanded
bytes, 10,000 members, 512 MiB compressed and expanded per leaf package/tarball,
64 MiB per receipt/manifest and 32 MiB per plan. Exceeding a ceiling fails; nothing
truncates or automatically expands a budget. Bounded vertical results do not
establish compatibility of every selected output/provider artifact with these
limits; the complete six-cell proof remains required.

## Implemented semantic boundary and remaining proof

The seal/stage/readback interface supports all six Core/Studio/Extensions × 3.8/3.9
schemas with source-bound specializations. The workflow has six explicit control
jobs, then fixed retrieval and independent readback. Successful actual Studio 3.8
and generator-bearing Extensions 3.8 verticals remain the required validation
sequence before complete matrix acceptance, not a workflow job dependency chain.
The transport-only manifest is not a substitute for the final semantic control
manifest. The implemented checks below repeat public byte/receipt joins; native,
compiler, test and runtime execution judgments remain inherited from the exact
successful source-bound receipts.

- Plan: closes the concrete public plan/inventory/metadata/history/feed schemas;
  admits the original plan hash at actual producer start and binds exact controller
  input hashes, original source policies, selected identities, expected archive
  names, output/symbol policies and dependency/framework groups. It reuses the pure
  `prove_product_release_artifacts.admit` and exact source contracts, without
  process, environment, Git or network calls in independent readback.
- Producer: closes the successful conditional writer, test counters and exact
  source-bound skip rows. It joins every selected package file/inventory/hash/size to
  actual ZIP bytes and nuspec ID/version/repository/dependency/framework metadata.
  It explicitly accounts for `packages.private_recipe_only_outputs`: only excluded
  plan IDs, exact version and safe relative inventories/hashes/sizes are allowed;
  excluded archive bytes are never staged. Original receipt bytes are preserved.
- Studio npm: the concrete historical writer has no embedded proof manifest or
  toolchain receipt. Invented fields are rejected. Its actual outer source,
  execution, command and lifecycle-correction schemas are closed. Checks recompute
  tarball inventories, package metadata hashes and SRI, validate names/versions
  and the exact React-to-WASM dependency, and join normal consumer local archive
  evidence and all three generated public
  asset families to WASM and wrapper bytes. They reuse the source-bound historical
  continuation contract; do not transplant the current Studio proof helper.
- Consumer: closes historical and complete current admissions separately, including
  checked time, exact counts and observation digest. It validates complete selected
  package/TFM bijection, output-only accounting, restore/input/isolation ledgers,
  raw versus native content hashes and original feed evidence. It joins every
  selected restored payload and runtime loaded asset to exact archive inventory
  bytes and validates representative runtime source/API pins and all applicable TFMs.
- Core: closes native per-asset assembly/PDB/symbol/document/satellite/SDK evidence
  against fixed trusted source contracts, preserving the exact SamplePackage
  no-symbol exception and original conditional skip policies. It never derives Core
  assembly versions from the requested NuGet version.
- Extensions: closes manifest/native SDK evidence, pins the generator/HTTP I/O
  representative policy and preserves original DLL `1.0.0+sourceSHA` separately
  from requested package version. Original manifest version conflicts remain
  failures. Native, compiler, SourceLink, runtime and test judgments are inherited
  from exact successful source-bound receipts, not rerun by Python.
- Final interface: builds the exact conditional file allowlist and final control
  manifest only after all semantic checks, preserving canonical input bytes. On
  original provider ZIP readback, it repeats every available semantic/hash/content
  join before writing the new readback receipt. The seal CLI and fixed workflow
  supply staging, retrieval and readback; this transport helper does not extract
  archives or perform provider requests.

The accepted architecture remains same-job private planning snapshot, producer
and cold consumer execution, followed by independent original provider-ZIP
readback. Actual complete six-cell acceptance remains unverified. This does not
claim a consumer executed after retrieval. Raw planning
assets, logs, TRX, caches, source trees and private paths remain outside uploads.
