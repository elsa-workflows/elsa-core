# Dependabot bounded discovery

Task [#8636](https://github.com/elsa-workflows/elsa-core/issues/8636) keeps the
October 8 owner decision permitting coordinated Core, Extensions and Studio
proposals with affected-product CI. The candidate replaces the single root solution
scan with five smaller scheduling units. It changes starting directories and cadence,
without changing package versions, project files, registries or publishers.

| Unit | Project-parent selectors | Projects | Daily time, Europe/Amsterdam |
| --- | --- | ---: | --- |
| Core production | `/core/src/**/*` | 114 | 06:00 |
| Core tests | `/core/test/**/Elsa.*` | 68 | 09:00 |
| Extensions | `/extensions/**/*` | 100 | 12:00 |
| Studio | `/studio/**/*` | 73 | 15:00 |
| Shared tooling | `/build`, `/.config` | 1 plus root tool manifest | 18:00 |

The recursive selectors match directories containing direct project entry points,
including deeper source, test and sample projects. No entry selects `/`, `Elsa.sln`
or a solution filter. Core tests use Elsa-named project parents so the separately
invoked `TlsSmoke` host stays outside coverage without relying on unverified exclusion
preprocessing. A new solution project with a differently named parent fails the
coverage gate and requires selector reconciliation. The other product selectors
also cover differently named parents at arbitrary depth.

Each unit retains both Feedz registry declarations, the `FEEDZ_API_KEY` secret name,
the `main` target, the `deps` commit prefix and the `Elsa*` group. Other dependencies
remain ungrouped. `open-pull-requests-limit: 1` applies to each job, so five version
update PRs can be open across these units. This is an explicit scheduling tradeoff;
staggering the jobs does not enforce a repository-wide cap or guarantee completion
within any time budget. Grouping within a job does not create one aggregate proposal
across jobs.

The root agent keeps one integration lane for these proposals. Before merging, it
reconciles versions and edits to shared central files, tool manifests and referenced
projects, then requires all affected-product CI and ordinary review/merge gates.
Core production, Core tests and build share the root central package file. Product
src/test/sample projects share their product central files, and references can cross
starting sets. Disjoint starting sets therefore do not promise disjoint writes or
atomic version alignment. Ancestor tool discovery can also expose the root tool to
multiple jobs; the explicit `/.config` entry preserves coverage without asserting
exclusive ownership.

## Complete tracked inventory

At baseline `6ba8e063f453efa22c40c79555a457aa202a496b`, `git ls-files` contains 379
C# project manifests. `Elsa.sln` names 355; the batching union selects those plus the
retained testing project.
The regression reconciles every tracked project, rather than the former 149 source-only
Extensions/Studio projects. All 173 Extensions/Studio projects are selected, including
20 Extensions tests, three Extensions samples and one Studio sample.

| Ownership | Tracked projects | In Elsa.sln | Selected outside Elsa.sln | Unselected |
| --- | ---: | ---: | ---: | ---: |
| Core | 183 | 182 | 0 | 1 |
| Extensions | 100 | 99 | 1 | 0 |
| Studio | 73 | 73 | 0 | 0 |
| Shared build | 1 | 1 | 0 | 0 |
| Integration proof tools | 22 | 0 | 0 | 22 |
| Total | 379 | 355 | 1 | 23 |

The unselected Core project is `core/test/TlsSmoke/TlsSmoke.csproj`, an isolated TLS
smoke host outside the product solution. It remains in the census, not claimed as
covered by these jobs. The 22 integration-program projects below are separately invoked
proof tools, including deliberately version-pinned historical upgrade consumers.
They retain their current solution/discovery disposition; this task does not change
their package versions or silently treat them as product coverage. Any addition or
removal outside the solution requires reconciling this explicit list:

- `scripts/integration-program/ProductReleaseSemantics/ProductReleaseSemantics.csproj`
- `scripts/integration-program/VerifyEmbeddedSources/VerifyEmbeddedSources.csproj`
- `scripts/integration-program/VerifyPackageSymbolPair/VerifyPackageSymbolPair.csproj`
- `scripts/integration-program/admission-package-consumer/AdmissionPackageConsumer.csproj`
- `scripts/integration-program/consolidation-identities/EvaluatedIdentityAudit.csproj`
- `scripts/integration-program/paired-blazor-host/UiProbe.csproj`
- `scripts/integration-program/paired-source-probe/ContractProbe.csproj`
- `scripts/integration-program/secrets-package-consumer/core-consumer/CoreConsumer.csproj`
- `scripts/integration-program/secrets-package-consumer/legacy-baseline/LegacyBaseline.csproj`
- `scripts/integration-program/secrets-postgresql-bridge/current-core/PostgreSqlBridgeRunner.csproj`
- `scripts/integration-program/secrets-postgresql-bridge/extensions-3.8.1/ExtensionsRunner.csproj`
- `scripts/integration-program/secrets-postgresql-upgrade/core-3.8.4/CoreRunner.csproj`
- `scripts/integration-program/secrets-postgresql-upgrade/extensions-3.8.1/ExtensionsRunner.csproj`
- `scripts/integration-program/secrets-sqlite-bridge-contract/core-3.8.4/CoreCryptoRunner.csproj`
- `scripts/integration-program/secrets-sqlite-bridge-contract/current-core/CurrentCoreBridgeRunner.csproj`
- `scripts/integration-program/secrets-sqlite-bridge-contract/extensions-3.8.1/ExtensionsCryptoRunner.csproj`
- `scripts/integration-program/secrets-sqlite-upgrade/core-3.8.4/CoreRunner.csproj`
- `scripts/integration-program/secrets-sqlite-upgrade/extensions-3.8.1/ExtensionsRunner.csproj`
- `scripts/integration-program/secrets-sqlserver-bridge/current-core/SqlServerBridgeRunner.csproj`
- `scripts/integration-program/secrets-sqlserver-bridge/extensions-3.8.1/ExtensionsRunner.csproj`
- `scripts/integration-program/secrets-sqlserver-upgrade/core-3.8.4/CoreRunner.csproj`
- `scripts/integration-program/secrets-sqlserver-upgrade/extensions-3.8.1/ExtensionsRunner.csproj`

The 356-project census does not count `.config/dotnet-tools.json`. The shared-tooling
unit explicitly selects `/.config`. At the baseline, the manifest contains
`dotnet-ef` version `9.0.11`; the coverage gate checks the tool identity and a nonempty
version without freezing future updates. It runs when this manifest changes.

## Apps consumer disposition

Task [#8746](https://github.com/elsa-workflows/elsa-core/issues/8746) adds exactly nine
registered Apps projects to the shared solution as **independent released-package consumers**.
The [Apps register](../apps-source-import.json) preserves their original manifests; the current
`Elsa.Apps.slnf`, tracked Apps projects and root solution must agree. They
remain outside these five source-package updater batches; the coverage census explicitly
reconciles the current consumer inventory rather than dropping it from inventory. Apps keeps its own
`apps/Directory.Packages.props`, `apps/NuGet.Config`, internal project references and
net10.0 target. Its local targets select only that restore config, prevent source-reference
substitution and set `IsPackable=false`.

The [Apps source workflow](../../../.github/workflows/apps-source-validation.yml) owns
root-context restore, product builds and the six nonpublishing image profiles. New Apps
source contributions route through Core. No Apps updater job or preview version stream
is activated here; any future updater authority/configuration decision remains separately
governed by [#8636](https://github.com/elsa-workflows/elsa-core/issues/8636) and
[operational program #8741](https://github.com/elsa-workflows/elsa-core/issues/8741).
The historical inventory table above predates these nine additions; the five selected
starting-set counts and existing release-unit inventory are unchanged.

## Templates product and build-data disposition

Task [#8748](https://github.com/elsa-workflows/elsa-core/issues/8748) retains nine Templates manifests outside the shared solution and the five updater batches. The explicit current inventory in `scripts/solution/solution-groups.json` separates the independent pack and test project from six embedded starter manifests and one standalone legacy host. Root `Elsa.Templates.slnf` selects `templates/Elsa.Templates.slnx`, with project paths relative to the product solution; root `Elsa.sln` has navigation items only. The census fails on unknown or unaccounted Templates projects, accidental shared-solution membership and build/data overlap. No Templates updater is activated.

Templates uses scoped props, CPM, targets and official-only NuGet configuration. Its [source workflow](../../../.github/workflows/templates-source-validation.yml) owns real pack/install/generate/build proof. The [import record](../templates-source-import.md) preserves the exact original65-file mapping and independent release/publisher handoff. The historical inventory table and five source-package starting-set counts above remain unchanged.

## Central package and build imports

The coverage test walks from each of the 356 selected project manifests to its nearest
`Directory.Packages.props`, then follows the literal import chain. Core and build reach
the root central file (207 PackageVersion elements); Extensions reaches
`extensions/src/Directory.Packages.props` (242), and Studio reaches
`studio/src/Directory.Packages.props` (74). Repeated elements can be conditional;
these counts are source metadata, not evaluated dependency versions.

The four `extensions/{test,samples}` and `studio/{test,samples}` central-file stubs
import their product's source central file. The regression validates all four,
including the currently project-free Studio test directory. CI reruns the check for
solution, project, central package, ancestor build policy, NuGet configuration and
SDK-selection changes. The test also resolves each selected project's nearest build
props/targets and
central file, follows repository imports (including `GetPathOfFileAbove` and
`MSBuildThisFileDirectory`), and checks literal project references against the selected
union. Unknown dynamic path forms fail closed. All three source framework declarations
remain `net8.0;net9.0;net10.0`; no framework is excluded by configuration. Static
XML/import reconciliation does not evaluate conditions, SDK imports, per-framework
central versions or restore/feed behavior.

Run the inexpensive coverage and configuration gate with:

```sh
ruby scripts/integration-program/test_dependabot_discovery.rb
```

The [Integration program tools workflow](../../../.github/workflows/integration-program-tools.yml)
runs this check. Its Ruby step uses bundled Minitest, with a pinned 5.16.3 fallback.
Synthetic regressions retain the terminal-glob failure and deeper-project case, and
exercise deeper project parents, the excluded TLS host, solution expansion, explicit
tool coverage and exact nonoverlapping batch membership against the root solution
plus the retained project. The inventory fixture copies every tracked `.sln` and
`.slnx`, so nested solutions expanding into another batch expose overlap. Native
`.proj`, `.fsproj` and `.vbproj` entry points fail closed until their model is
reconciled, with CI triggers covering those additions. A future differently named Core test fixture demonstrates
the selector boundary. The matcher/solution model corroborates selection; it is not
native updater execution or a replacement for the next gates.

## Native and hosted acceptance

The historical failed-discovery receipts are Extensions
[run 37467995784](https://github.com/elsa-workflows/elsa-core/actions/runs/37467995784),
which recorded NU1008/NU1010 for the retained testing project, and Studio
[run 37467996957](https://github.com/elsa-workflows/elsa-core/actions/runs/37467996957),
which discovered no product projects. Green job status with empty project discovery
was not successful product evaluation. These receipts differ from the later cancelled
jobs below and do not establish native or hosted acceptance.

The failed hosted discovery attempts used updater `9c06d60057ba9e7e79210e6618f932a28cf6a158`.
Its [PathHelper](https://github.com/dependabot/dependabot-core/blob/9c06d60057ba9e7e79210e6618f932a28cf6a158/nuget/helpers/lib/NuGetUpdater/NuGetUpdater.Core/Utilities/PathHelper.cs#L266)
expands job directories; [DiscoveryWorker.FindEntryPoints](https://github.com/dependabot/dependabot-core/blob/9c06d60057ba9e7e79210e6618f932a28cf6a158/nuget/helpers/lib/NuGetUpdater/NuGetUpdater.Core/Discover/DiscoveryWorker.cs#L262)
scans direct files and expands supported solutions, not `.slnf` files. Terminal `/**`
does not recurse to project manifests. The recursive-directory correction in
[PR #8640](https://github.com/elsa-workflows/elsa-core/pull/8640) fixed that miss, but
the later product jobs reached the 55-minute limit. The pinned native synthetic
proposal diagnostic showed cross-product central-file changes and fewer discovery
calls for a solution; it was not full-repository or byte-identical hosted proof.

Fresh evidence narrows the remaining gap: [PR #8673](https://github.com/elsa-workflows/elsa-core/pull/8673),
created October 8, contains three actual version changes in `studio/src/Directory.Packages.props`.
It establishes a Studio proposal, not complete project/import evaluation. Later
NuGet jobs [38003504544](https://github.com/elsa-workflows/elsa-core/actions/runs/38003504544)
and [37883354734](https://github.com/elsa-workflows/elsa-core/actions/runs/37883354734)
were cancelled. Their status does not supply the missing full-inventory acceptance.

The later root-solution [managed run 38108457632](https://github.com/elsa-workflows/elsa-core/actions/runs/38108457632)
on `6ba8e063f453efa22c40c79555a457aa202a496b` was cancelled after about 55 minutes.
Its 175 project strategy starts cover shared build plus 114 Core production and 60
Core test projects. They are starts, not completed project evaluation receipts; no
Extensions or Studio strategy start was present before cancellation. Distributed
progress supports reducing concentrated work, without establishing a batch runtime.

The reported updater OCI tag is `d3bc6c74cbd106d79d24a728ab623a3eaa776087`.
Its [PathHelper](https://github.com/dependabot/dependabot-core/blob/d3bc6c74cbd106d79d24a728ab623a3eaa776087/nuget/helpers/lib/NuGetUpdater/NuGetUpdater.Core/Utilities/PathHelper.cs)
and [DiscoveryWorker](https://github.com/dependabot/dependabot-core/blob/d3bc6c74cbd106d79d24a728ab623a3eaa776087/nuget/helpers/lib/NuGetUpdater/NuGetUpdater.Core/Discover/DiscoveryWorker.cs)
source files are byte-identical to those at the older diagnostic source pin. They
confirm recursive directory matching and direct-file entry discovery. This source
identity does not establish equivalence to the executed helper binary or its full
source/submodule closure. The Ruby matcher is grounded in this source pin.

The same source's [MSBuildHelper](https://github.com/dependabot/dependabot-core/blob/d3bc6c74cbd106d79d24a728ab623a3eaa776087/nuget/helpers/lib/NuGetUpdater/NuGetUpdater.Core/Utilities/MSBuildHelper.cs)
searches ancestors for the tool manifest. [SDK project discovery](https://github.com/dependabot/dependabot-core/blob/d3bc6c74cbd106d79d24a728ab623a3eaa776087/nuget/helpers/lib/NuGetUpdater/NuGetUpdater.Core/Discover/SdkProjectDiscovery.cs)
uses restore targets including `ResolveProjectReferences` and returns framework,
reference and import records. Repository paths outside a starting directory can thus
participate; static closure checks do not substitute for those evaluated records.
Selecting project parents removes the root solution workspace and its synthetic
`SolutionDir`; the static check found no `SolutionDir` usage in repository project,
props or targets files. SDK behavior remains a native acceptance concern.

Before a corrected hosted attempt, root independently reviews this configuration and
coverage proof. Then use the reviewed pinned native discovery interface on an isolated
snapshot of every actual scheduling unit. Reconcile evaluated project records, all
frameworks, central versions and imports/references against the union above. Record
failures and omissions explicitly, including duplicate shared-file/tool discovery.
Do not replace native evidence with this Ruby model or increase the old timeout.

`extensions/src/Elsa.Testing.Extensions/Elsa.Testing.Extensions.csproj` remains
selected without retirement. Earlier native/hosted evaluation returned `Projects=[]`,
`IsSuccess=true`, `Error=null` after NU1008/NU1010 warnings: the then-current project's
explicit conditional `Microsoft.AspNetCore.Mvc.Testing` versions conflicted with
inherited central management, and its net7.0 target lacked some inherited central
versions. The warnings continued rather than aborting discovery.

The source repair removes those local framework/version overrides. The helper now
inherits net8.0, net9.0 and net10.0, with central MVC.Testing versions 8.0.17, 9.0.17
and 10.0.9 respectively. Splitting the former shared net8/net9 MVC row increases the
Extensions central-file element count from the baseline 242 to 243. The 19 module-test
consumers remain on net10.0 and MVC.Testing 10.0.9. The `retained-testing-helper` CI job
restores and builds the otherwise out-of-solution helper on all three frameworks and
checks resolved MVC.Testing versions against evaluated central policy. This source
repair and build check do not establish native updater success; current native
evaluation must still report the helper's disposition without omissions.

After the correction lands on default `main`, Task #8636 still needs terminal actual
managed results for every unit, complete evaluated discovery and central-version
evidence
for the selected union, an update or current-version result, and configured Feedz
resolution where exercised.
Generated dependency PRs follow ordinary affected-product CI/review gates. A green
Actions status, a generated proposal, or local selector coverage alone does not accept
the Task. No new hosted run, credential test or publication is performed by this change.
