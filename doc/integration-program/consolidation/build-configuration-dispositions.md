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
package-mode restore, compile, pack, or consumer-compatibility result. Keep the
asset pending until #8260 records that evidence; do not treat the source-build
default as package or publisher policy.

The Extensions NuGet config maps `Elsa` and all `Elsa.*` IDs to Elsa preview
Feedz. Core's active mapping is narrower: the wildcard routes to NuGet.org and
only `Elsa.Platform.PackageManifest.Generator` and
`Elsa.Platform.PackageManifest` are assigned to Elsa Feedz. Unioning the old
pattern would change package-source selection for every matching ID and could
change which feed is trusted to supply package-mode dependencies. No clean
package-consumer restore under the active root config or feed-ownership review
was found in the checked build-preparation evidence. Therefore keep the Core
mapping active and do not add the broad Extensions pattern yet. #8260 should
record the exact package IDs needed by the package-mode proof, the source that
supplies each, and the trust/ownership decision before this ledger row is
completed.

Both ledger rows remain pending. The proposed dispositions describe the
active-layout direction, not completed package compatibility. A future
reviewed ledger update must carry the evidence and completion fields required
by the validator. The source `.source` copies remain until then.
