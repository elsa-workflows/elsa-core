# Selected Core payload specialization

`scripts/integration-program/selected_core_payload.py` supplies
`validate_specialization(plan, producer_receipt, consumer_receipt,
selected_archives, *, contracts)` for the shared selected-control semantic seal.
It returns `None` on acceptance and raises `ValueError` on a mismatch. This is a
validator, not a producer, consumer, publisher, or independent native execution.

The caller first admits strict JSON, closes the common envelopes, binds the
controller and source, verifies retained nupkg/snupkg bytes and their exact safe
member inventories, checks nuspec identity, provenance, dependencies and framework
references, and validates complete coverage and restored payload byte joins. Each
`selected_archives` value is exactly `{record, members, policy, data}`: its nupkg
record, safe member names mapped to immutable bytes, unchanged selected plan row,
and exact archive bytes. Keys are case-folded package IDs. Paired snupkg records
remain in `producer_receipt.packages.selected`; the common layer checks those
bytes and inventories too.

The separate `load_contracts()` reads only the fixed, tracked
`selected_core_contract.json`, `selected_core_consumer_contract.json` and
`core_source_continuation_contract.json` beside the trusted controller module and
returns `{producer, consumer, continuation}`. The specialization
receives those trusted values explicitly. Validation reads no files or environment,
launches no processes, and makes no network requests. Artifact-directed paths are
validated as relative names and never opened.

The Core checks cover both pinned original sources, the original release recipe
and test census, absence of Core npm fields, and the preserved
`private_recipe_only_outputs` metadata partition. Recipe-only rows are allowed
only for IDs declared in the exact plan's exclusion inventory and the requested
version. Closed safe filename/member inventories retain hashes and sizes, while
excluding archive/member bytes, raw private paths, logs and cache fields. Their
IDs and files are disjoint from selected archives; paired excluded nupkg/snupkg
metadata may share an ID. Empty output is valid but is not an invariant inferred
for either original Core recipe. This metadata is preserved without rewriting it;
its private archive bytes are not independently available for rehashing. The
shared layer forbids those excluded bytes in the public staged file set.
Every native package row is closed and
joins to the selected archive records, emitted DLLs, satellites, paired PDB
inventory, manifest bytes and SDK build/manifest assets. Assembly versions come
from native producer rows rather than being synthesized from the NuGet version.
The exact no-symbol exception remains `Elsa.SamplePackage` at
`src/apps/Elsa.SamplePackage/Elsa.SamplePackage.csproj`. Its private PDB has retained
native identity/hash/size evidence, but no retained PDB bytes. Output-only policy
requires no emitted framework, DLL, satellite or symbol rows; the archive still
requires common consumer coverage. The currently observed Core plans contain no
output-only selected package; that policy edge is synthetic test coverage.

Tracked SourceLink documents use the original writer's `original-git` branch with
the exact canonical repository URL and pinned commit, safe relative path and
checksum. Original Core does **not** set the imported maintenance `source_kind=core`
flag, and its writer does not require remote fetching or emit `remote_fetched`.
The validator does not invent remote availability evidence. Embedded generated
documents retain the closed SDK, framework or NuGet producer branch and source
family. An empty document list requires the exact native no-executable-method-body
applicability counts; it is not an unexplained source-evidence omission.

The retained Core test projection must contain exactly the source-pinned 44 (3.8)
or 61 (3.9) project/net10.0 cells, no inherited placeholder rows, all 16 integer
counters, positive passed counts and exact fixed skip identities/reasons.
Conditional provider skips must form complete source-bound provider groups; each
whole group may be present or absent. The public receipt does not retain provider
connection strings or nonsecret gate flags. Therefore the validator checks the
legitimate group alternatives and counters without reading current environment or
claiming to independently establish which provider gates were enabled. Skips are
counted separately from passed tests.

Representative runtime rows cover Elsa on net8.0/net9.0/net10.0 and bind the exact
source contract map and native writer's workflow description. Required loaded
assemblies are Elsa, Elsa.Workflows.Core and Elsa.Workflows.Runtime. Every selected
loaded assembly joins its framework-specific archive DLL, native SHA and original
assembly/informational versions, and the runtime restored-payload row. The actual
workflow checks (nearest variable scope, exact `Sequence Value` output,
Finished/Finished and zero incidents) were enforced by the executed source-pinned
consumer. Their raw result is not retained, so matching the retained description
and source contract is an inherited execution claim, not a re-execution from JSON.

Recomputed checks are byte hashes and joins plus the closed retained schemas.
MSBuild evaluation, private source/satellite bytes, Portable PDB interpretation and
pairing, original source checksum interpretation, TRX linkage/outcome parsing,
native NuGet semantics, actual fresh-cache restore/build and workflow execution
remain inherited from successful source-pinned controls. The producer discards its
fresh admission return value; the consumer retains only a summary and digest of
private admission observations. This specialization does not independently
re-evaluate either current admission from those public projections.

Tests use bounded synthetic package/native shapes and all required source-contract
test cells. They do not certify either complete 93/103 package release inventory,
real PDBs/TRX files, actual native execution, hosted provider authority, or any of
the six required selected controls. The root integration and actual controls remain
responsible for those proof boundaries.

### Fixed Core source continuations

The [fixed Core continuation route](product-release-plans.md#fixed-core-source-continuations)
adds a trusted continuation catalog alongside the original producer/consumer contracts.
The pure validator derives the original recipe/test policy plus only the exact delta,
checks candidate SamplePackage metadata, and preserves native archive, SourceLink,
assembly and runtime checks. Historical original-source fixtures remain unchanged.
