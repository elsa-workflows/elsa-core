# Temporary #8636 native batch diagnostic

The manual Integration program tools workflow exposes `native_diagnostic_mode`:
`off` (ordinary checks), `enumerate`, `tooling`, `retained-probe`, or `product-smoke`. A non-off
choice runs only the diagnostic job. Root must review this adapter and authorize
execution separately. Remove the proof helpers and opt-in job after their evidence
has been accepted and retained. No local native execution is needed for review.

There are no target, source, SDK, selector, or arbitrary unit overrides. The target
is `cc7ee0bd6a1ceb306e3dbadae1a4f3bf83e4efea`, tree
`e10dd15f97def82b832e95855d65d37760633be2`, with Dependabot configuration blob
`83e586893f4e58a5f938db815d5478afeb5651b0`. The archive, initial tracked content,
source closure, caller sources, reconstructed caller/helper binaries, SDK, and gitlinks receive
identity receipts. The helper uses upstream Dependabot source
`d3bc6c74cbd106d79d24a728ab623a3eaa776087`, NuGet.Client
`626dc64e95d6f5bc366cbbe191a15751ccbfb097`, dotnet/core
`843c037496b2941d8bc42d0eceaabf8d15a9deff`, and SDK `10.0.400` exactly.
The unchanged official CLI and thin caller are built once within each attempt.
`reconstructed_binary_sha256` records pre-execution disk hashes; it does not prove
which assemblies a runtime loaded. This source/binary identity does not establish equivalence with the
managed updater image or its executed helper binary.

Every mode first derives the five exact directory-selector lists from the pinned
YAML and calls public pinned `Job.GetAllDirectories` on the full snapshot:

| Unit | Selectors | Selected projects |
| --- | --- | ---: |
| Core production | `/core/src/**/*` | 114 |
| Core tests | `/core/test/**/Elsa.*` | 68 |
| Extensions | `/extensions/**/*` | 100 |
| Studio | `/studio/**/*` | 73 |
| Tooling | `/build`, `/.config` | 1 |

`enumerate` stops after native directory matching and direct entrypoint inventory.
It never calls `DiscoveryWorker` or restores target projects. Native directory
order and project-free directories are retained. The receipt reconciles all 379
tracked projects, 355 solution entries, the retained testing project (356 selected
total), all 173 Extensions/Studio projects, and the 23 outside projects. Selected
project starts must be disjoint and exactly match the five unit inventories. Root
or unknown directories and selected solution/unsupported entrypoints fail closed;
`Elsa.sln` is never selected. This establishes matching and inventory only, not
MSBuild evaluation or import/reference closure.

`tooling` then calls public `DiscoveryWorker.RunAsync` separately for `/build` and
`/.config`. `retained-probe` calls it only for
`/extensions/src/Elsa.Testing.Extensions`. There is no automatic five-unit discovery
loop. Each workspace gets an atomic started/returned/threw receipt immediately at
its call boundary. Per-workspace root `DotNetToolsJson` observations retain only
`dotnet-ef`, a validated version, known manifest path, success/error enums, and a
comparison with the exact target manifest. The current target version is 9.0.11;
version updates are not prohibited by the schema. Shared ancestor tool discovery
may produce repeated observations, which are preserved for later reconciliation.
An empty `.config` project list alone does not establish tool coverage.

`product-smoke` is one fixed pair: `extensions/src/email/Elsa.Email` and
`studio/src/framework/Elsa.Studio.Core`. Both must appear in the actual native
Extensions/Studio directory and direct-entrypoint inventories. Python invokes the
caller with an internal index restricted to 0 or 1; it is not a public workspace
or unit override. The first native result and its evaluated inputs must be valid before the second
call starts. Missing, empty, failed or unsafe results stop the attempt. If the only
gaps are known native conditional-version attribution loss, the second fixed
project is still collected; semantic completeness remains false. There is no full-unit discovery loop.

For each fixed project, three `dotnet msbuild` evaluation queries use only
`-property:TargetFramework=net8.0`, `net9.0`, or `net10.0`, the fixed
`-getProperty:TargetFramework,TargetFrameworks,ManagePackageVersionsCentrally,DirectoryPackagesPropsPath`
and `-getItem:PackageVersion,PackageReference,GlobalPackageReference,ProjectReference`. They do not run
restore/build targets and do not prove package resolution. Raw query output stays
private. The closed receipt retains known central/package/project paths, admitted
package IDs, validated versions/VersionOverride, defining-file identity, evaluated
CPM applicability, references and exact TFM attribution. SDK implicit references
are counted separately; their arbitrary IDs or SDK paths are not copied.

Native records preserve Name/Version/Type/TargetFrameworks/IsTopLevel, nullable
EvaluationResult presence and its exact ResultType enum (Success, PropertyIgnored,
CircularReference, PropertyNotFound), and known
project-relative imports/reference edges, including duplicate edges and additional
tracked closure project records. The join checks each evaluated direct dependency
against a native top-level package declaration with the same version and TFM, and
compares native central identity/imports and reference edges with evaluation. A
native aggregate that loses a conditional framework/version association stays
unresolved; aggregate project TFMs or literal ancestor XML values do not repair it.
Unknown imports/references/top-level dependency fields, closure failures and
unevaluable inputs block semantic matching. Only well-shaped unadmitted transitive
dependencies are separately counted and omitted. Raw expression values, property
names, error text and InfoUrl are excluded. Evaluated globals retain their item
kind and explicit version; pinned SDK native discovery emits those declarations
as PackageReference, so that native type is admitted for a global join without
pretending native output distinguishes the original item collection.
No claim of complete closure evaluation follows from retaining its records.
`semantic_preflight_complete` describes this fixed pair only; all 356 selected and
173 imported-product dispositions/denominators remain visible and acceptance stays
false. Neither runtime feasibility of an entire scheduling unit nor operational
acceptance follows from a passing smoke.

Every probe keeps explicit dispositions for all 356 selected projects, including
unrequested and omitted paths. Retained source is outside `Elsa.sln` and has
explicit `net7.0`, `net8.0`, and `net9.0` targets. Historical publication eligibility
does not decide updater eligibility. Its known CPM errors (NU1008/NU1010) and an
empty successful workspace result do not prove evaluation; no framework, reference,
package, or project is changed or pruned to make it pass. The current reconciler
reports observed safe TFMs and literal central-version membership only. It cannot
certify evaluated conditional versions, complete per-TFM results, or reference
closure. Complete per-TFM reconciliation, duplicate/ancestor tool handling, Feedz
request-versus-resolution evidence, terminal proposals/current-version results,
and managed equivalence remain later gates. No denominator or acceptance is waived.

This is a credential-free diagnostic: child processes receive an explicit minimal
environment with private HOME, DOTNET_CLI_HOME, NuGet packages/HTTP caches and temp
folders. User NuGet credential variables, GitHub tokens, runner secrets and user
Git configuration are not inherited. No feed credentials or hosted job dictionary
are supplied; feed acceptance remains unknown. Raw stdout/stderr, native JSON,
exceptions and command arguments remain private in RUNNER_TEMP. Evidence reads and hashing reject linked files and linked parent directories,
including links within the snapshot. Missing/unsafe files fail content rechecks.
Only the closed
summary is uploaded for seven days. Unknown strings, URLs, config values and error
messages are excluded; known contained paths, IDs, validated versions/TFMs, enums,
counts, timing and digests are retained.

The existing 1,800-second budget is unchanged: work stops at 1,740, final readback
and content checks stop before 1,785, and the hard watchdog fires at 1,790. Owned
process groups receive TERM then KILL with a five-second grace. Atomic summaries
and per-workspace receipts survive interruption; safe incremental progress reads
at most 64 KiB per poll, bounds its journal, and marks truncation/rotation gaps.
Restore-strategy markers mean a strategy was selected, not restore completion.
Missing markers or output remain unknown. No optional process sampler is added.
The existing 45-minute workflow cap leaves artifact-upload time.

`attempt_complete` and exit zero describe only successful execution and receipt
reconciliation of the selected bounded stage, including content rechecks and call
returns. Product-smoke may finish its bounded receipts with native-attribution gaps;
`semantic_preflight_complete` remains false in that case. For tooling/retained modes they do not require successful project evaluation: a returned empty or
failed project result remains explicitly unverified. `diagnostic_complete`,
`all_selected_native_evaluation_complete`, `managed_acceptance`, and `acceptance`
always remain false in this adapter, including successful enumeration. A failed
or timed-out attempt exits nonzero and preserves available safe evidence.

Cheap checks from repository root:

```sh
python3 -m unittest discover -s scripts/integration-program -p test_dependabot_native_proof.py
python3 -O -m unittest discover -s scripts/integration-program -p test_dependabot_native_proof.py
actionlint .github/workflows/integration-program-tools.yml
git diff --check
```

The tests use synthetic native records and short Python subprocesses, not native
execution. Root owns independent review, execution approval, and all later gates.
