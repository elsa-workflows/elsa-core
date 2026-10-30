# Extensions root build configuration dispositions

Checked against Core `origin/main` and the history-import candidate at
`f7f6c02637546740d71ae4d2a53490cb829067ef` on 2026-09-25. Both roots have the
same active configuration blobs in those trees:

| Active Core path | Blob | Observed policy |
| --- | --- | --- |
| `Directory.Build.targets` | `2a61364c98e134dfbbf6ba8c5d6498f0bffcb81b` | Sets `UseProjectReferences=true` and includes Core's `ConfigureAwait.Fody` validation target. |
| `NuGet.Config` | `1ec3544b7f8af520075affc7cf0e32cbc768476a` | Uses NuGet.org, Elsa preview Feedz, and Loom Feedz. Wildcard, CShells, and CShells.* map to NuGet.org; only the two PackageManifest IDs map to Elsa preview Feedz; Loom IDs map to Loom Feedz. |

The archived Extensions inputs are still the exact files at source commit
`33fa0bfd28c7585240e3d4f665058c067b17e287`: `Directory.Build.targets`
(`143bfd8dc2fef88c095179b0a28bfdcee92ad123`) and `NuGet.Config`
(`e3f020ab738262a079598ac58a5d22d6429db064`). Their archive paths remain
`doc/integration-program/legacy/extensions/Directory.Build.targets.source` and
`doc/integration-program/legacy/extensions/NuGet.Config.source`; this record
does not rewrite or remove those provenance files.

The Extensions target sets `UseProjectReferences=false`, opposite to the
consolidated Core default. Retire that Extensions-specific default from the
active monorepo layout: the shared true setting supports backend and Studio
source development together. A package consumer can still explicitly evaluate
the false mode. The existing build-preparation notes show both evaluated
reference modes and document Slack's `PackageReference` on `Elsa` at
`ElsaVersion` `3.8.0-preview.5557`, but they explicitly do not establish a
package-mode restore, compile, pack, or consumer-compatibility result. At that
snapshot the asset stayed pending; the newer bounded proof follows. Do not
treat the source-build default as package or publisher policy.

## Bounded Slack package-mode result

On 2026-09-25, the reviewed draft source at
`55673b1dd32fd7c8dd1c92f53321023376cfa778` was evaluated without changing
the build inputs inherited from its parent
`11956a58dc7d88f9b08b958242fecf25c2db7a0e`. For
`src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj`, default MSBuild
evaluation reported `UseProjectReferences=true`, one reference to the mapped
Core `Elsa.csproj`, and no `Elsa` package reference. With explicit
`-p:UseProjectReferences=false -p:ElsaVersion=3.8.4`, it reported no project
references and one `Elsa` package reference. This is the deliberate
source-debug versus released-Core-package switch; the old Extensions default
`false` would defeat source co-development if copied into the root.

A clean, isolated `NUGET_PACKAGES` cache then restored that package mode with
the **active root** `NuGet.Config`, `--force-evaluate` and `--no-cache`.
NuGet `.nupkg.metadata` recorded `Elsa` 3.8.4 and `SlackNet` 0.17.7 from
NuGet.org; the assets graph contained both packages for net8.0, net9.0 and
net10.0. A Release `--no-restore` build passed for all three frameworks with
zero errors and three existing NU1902 warnings for
`Microsoft.Build.Tasks.Git` 8.0.0. No pack or publication ran in this
root-config check. Separately, draft-import child
[#8436](https://github.com/elsa-workflows/elsa-core/pull/8436) passed an
artifact-only pack and clean net8/9/10 consumer on exact head
`9e19c4d49f1c2ede6f11c442732e151597c3efa0` using a NuGet.org-only
configuration; its package and symbol provenance are recorded in
[#8260](https://github.com/elsa-workflows/elsa-core/issues/8260).

Reproduce the root-config restore/build on the pinned source with a new empty
cache directory (replace `/path/to/empty-cache` with its absolute path):

```sh
dotnet msbuild src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj \
  -getProperty:UseProjectReferences -getItem:PackageReference \
  -getItem:ProjectReference -p:UseProjectReferences=false -p:ElsaVersion=3.8.4
NUGET_PACKAGES=/path/to/empty-cache dotnet restore \
  src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj \
  --configfile NuGet.Config --force-evaluate --no-cache \
  -p:UseProjectReferences=false -p:ElsaVersion=3.8.4
NUGET_PACKAGES=/path/to/empty-cache dotnet build \
  src/extensions/communication/Elsa.Slack/Elsa.Slack.csproj \
  --configuration Release --no-restore \
  -p:UseProjectReferences=false -p:ElsaVersion=3.8.4
```

This closes the bounded Slack evidence gap for retiring the old Extensions
`Directory.Build.targets` default. The ledger now records its active Core-root
representation at blob `2a61364c98e134dfbbf6ba8c5d6498f0bffcb81b`,
backed by reviewed [PR #8481](https://github.com/elsa-workflows/elsa-core/pull/8481)
and merge `ad1c58038ab75b46f9828ead9ea364b410ea9f8f`. The old default is
retired as policy; the shared root target remains active, so this is a
represented asset in the ledger. Other Extensions package-mode graphs and the
final import are not proved by this one connector.

The Extensions NuGet config maps `Elsa` and all `Elsa.*` IDs to Elsa preview
Feedz. Core's active mapping is narrower: the wildcard routes to NuGet.org and
only `Elsa.Platform.PackageManifest.Generator` and
`Elsa.Platform.PackageManifest` are assigned to Elsa Feedz. Unioning the old
pattern would change package-source selection for every matching ID and could
change which feed is trusted to supply package-mode dependencies. No clean
package-consumer restore under the active root config or feed-ownership review
was found in the earlier build-preparation evidence. The Slack result above now
proves root-config restore for **that** release unit, not every imported
Extensions package or a live publisher feed. Therefore keep the Core mapping
active and do not add the broad Extensions pattern yet. Broader package-mode
checks and source trust/ownership review are required before this NuGet config
ledger row is completed.

The target row is represented in Core; the old NuGet mapping remains pending.
It still needs broader package-mode and feed-ownership evidence. The source
`.source` copies remain.
