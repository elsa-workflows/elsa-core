# Dependabot solution discovery

Task [#8636](https://github.com/elsa-workflows/elsa-core/issues/8636) replaces three
overlapping NuGet scans with one coordinated job. The October 8 owner decision
permits proposals spanning Core, Extensions and Studio, with affected-product CI.
The job selects `/` (the actual `Elsa.sln`) and
`/extensions/src/Elsa.Testing.Extensions` (the retained project outside that solution).
No solution filter is used as an updater entry point.

The root solution gives the native updater one workspace containing product source,
tests, samples and shared build tooling. Removing the root product exclusions allows
central imports and project references to participate in coordinated proposals.
The retained project stays visible; selection does not establish successful evaluation.

The single job retains both Feedz registry declarations and the `FEEDZ_API_KEY`
secret name, the `main` target, and the `deps` commit prefix. It uses the existing
product cadence, daily at 06:00 Europe/Amsterdam, so Core changes from weekly to daily.
The one-open-PR limit now applies to the coordinated job instead of separately to
the two product jobs. The existing `Elsa*` group now also covers Studio and Core;
other dependencies remain ungrouped. These are proposal settings, not publication
or automatic-merge policy. No package version, project or publisher changes.

## Complete tracked inventory

At baseline `3cc81c652906fc0e18a2ec0c6732308440f8c720`, `git ls-files` contains 379
C# project manifests. `Elsa.sln` names 355; the additional directory selects one more.
The regression reconciles every tracked project, rather than the former 149 source-only
Extensions/Studio projects. All 173 Extensions/Studio projects are selected, including
20 Extensions tests, three Extensions samples and one Studio sample.

| Ownership | Tracked projects | In Elsa.sln | Additional directory | Unselected |
| --- | ---: | ---: | ---: | ---: |
| Core | 183 | 182 | 0 | 1 |
| Extensions | 100 | 99 | 1 | 0 |
| Studio | 73 | 73 | 0 | 0 |
| Shared build | 1 | 1 | 0 | 0 |
| Integration proof tools | 22 | 0 | 0 | 22 |
| Total | 379 | 355 | 1 | 23 |

The unselected Core project is `core/test/TlsSmoke/TlsSmoke.csproj`, an isolated TLS
smoke host outside the product solution. It remains in the census, not claimed as
covered by this job. The 22 integration-program projects below are separately invoked
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

## Central package imports

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
SDK-selection changes. Static XML/import reconciliation does not evaluate MSBuild
conditions, restore packages or prove Feedz authentication.

Run the inexpensive coverage and configuration gate with:

```sh
ruby scripts/integration-program/test_dependabot_discovery.rb
```

The [Integration program tools workflow](../../../.github/workflows/integration-program-tools.yml)
runs this check. Its Ruby step uses bundled Minitest, with a pinned 5.16.3 fallback.
Synthetic regressions retain the terminal-glob failure and deeper-project case, and
exercise solution expansion through source, test and sample entries plus the retained
direct project. The matcher/solution model corroborates selection; it is not native
updater execution or a replacement for the next gates.

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

Before the corrected hosted attempt, use the pinned native discovery interface on
an isolated snapshot of this exact solution and retained entry point. Reconcile its
project records, evaluated central versions and import paths against the complete
inventory above. Record failures/omissions explicitly; do not replace native evidence
with this Ruby model or increase the old directory job's timeout.

`extensions/src/Elsa.Testing.Extensions/Elsa.Testing.Extensions.csproj` remains
selected without retirement. Earlier native/hosted evaluation returned `Projects=[]`,
`IsSuccess=true`, `Error=null` after NU1008/NU1010 warnings: its explicit conditional
`Microsoft.AspNetCore.Mvc.Testing` versions conflict with inherited central management,
and its net7.0 target lacks some inherited central versions. The warnings continued
rather than aborting discovery. Current native evaluation must report its disposition;
this configuration repair does not fix its project or claim the warnings resolved.

After the correction lands on default `main`, Task #8636 still needs a completed
hosted discovery/evaluation result, central-version evidence for selected projects,
an update or current-version result, and configured Feedz resolution where exercised.
Generated dependency PRs follow ordinary affected-product CI/review gates. A green
Actions status, a generated proposal, or local selector coverage alone does not accept
the Task. No new hosted run, credential test or publication is performed by this change.
