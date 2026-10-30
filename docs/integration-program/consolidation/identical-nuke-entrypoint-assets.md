# Identical Extensions NUKE entrypoint assets

Program #8194; Feature #8214; Story #8286. Five retained Extensions build
assets are already present at the active Core root with the exact same Git
blob and mode. The comparison uses the frozen Extensions source pin
`33fa0bfd28c7585240e3d4f665058c067b17e287`, its inert `.source` copies
in import draft #8409 at `530d9489e49a45c66e6922d6fcf597dade0aa5cd`,
and Core `main` at `34de0aa24bb785cd0b0ed4d5a237efb19dda2122`.

| Extensions asset and active Core path | Git blob | Mode |
| --- | --- | --- |
| [`build.cmd`](../../../build.cmd) | `b08cc590f4c39e05283116619a75b76a6010c539` | `100755` |
| [`build.ps1`](../../../build.ps1) | `4634dc03e9f83f93e8ade1957547e0320fba0332` | `100644` |
| [`build.sh`](../../../build.sh) | `fdff0c623663cbbe1226165447a1a7e1d06d042e` | `100755` |
| [`build/.editorconfig`](../../../build/.editorconfig) | `31e43dcd8e5b5114d731ab0172aceeb7d7c2edb9` | `100644` |
| [`build/_build.csproj.DotSettings`](../../../build/_build.csproj.DotSettings) | `c815d363e82b2e04ddc6c04638996d6b5f8ffb90` | `100644` |

The three root wrappers invoke the selected root NUKE build; their source
copies are exact duplicates, so no second active wrapper is needed. The root
`.nuke/parameters.json` intentionally selects `Elsa.sln`, as recorded in the
[separate NUKE configuration decision](nuke-build-asset-dispositions.md).
The import draft's exact-head [full CI](https://github.com/elsa-workflows/elsa-core/actions/runs/36142805569)
ran the root build on Linux. Byte identity establishes that the PowerShell
and Windows wrapper source was preserved, but this decision does not claim a
new Windows execution result. The two build-tool editor files also require no
copying or formatting transformation.

This classification does not settle the different `build/Build.cs`,
`build/_build.csproj`, NUKE build-tool properties/targets, package publishing,
or independent release units. They remain separate ledger rows and gates.
No active build, package or workflow file changes in this decision.

Reproduce with `git ls-tree HEAD build.cmd build.ps1 build.sh
build/.editorconfig build/_build.csproj.DotSettings` in Core and the
corresponding `doc/integration-program/legacy/extensions/` `.source` paths in
the checked-out import draft. `cmp` of each active path and its `.source`
counterpart returns zero in that draft.
