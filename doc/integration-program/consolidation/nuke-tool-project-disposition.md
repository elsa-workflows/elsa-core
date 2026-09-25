# Extensions NUKE tool-project disposition

Program #8194; Feature #8214; Story #8286. The retained Extensions NUKE
tool configuration is represented by Core's active `build/` project. The
frozen Extensions pin is `33fa0bfd28c7585240e3d4f665058c067b17e287`;
the same three original files are unchanged at Extensions main
`9361c80e2ea56ccf71118fb53d6fd8f3d0fbbbf0`. Their inert `.source`
copies are in import draft #8409. Active Core blobs were checked at
`2616c7beb9c946c98c9f96b691128029f7799746`.

| Retained Extensions asset | Active Core representation | Decision |
| --- | --- | --- |
| `build/Directory.Build.props`, blob `61fd9f44b85b914b0d3d8996c055de86b67b3026`, `100644` | [`build/Directory.Build.props`](../../../build/Directory.Build.props), same blob and mode | Retain the build-project-local `UseArtifactsOutput=false`; do not make it a repository-wide property. |
| `build/Directory.Build.targets`, blob `253260956d504935e80ddc02a0695a148d9f2a1e`, `100644` | [`build/Directory.Build.targets`](../../../build/Directory.Build.targets), same blob and mode | Retain the local boundary against unintended parent target imports. |
| `build/_build.csproj`, blob `24319083da5c04240f5da58037d5a4895bc70196`, `100644` | [`build/_build.csproj`](../../../build/_build.csproj), blob `e35f2122925738488ac39a1a75e2ca6f21d8a416`, `100644` | Retain the scoped NUKE executable project with `Nuke.Components` 10.1.0, `NuGet.Packaging` 7.9.0, and an advanced `System.Security.Cryptography.Xml` override, 10.0.10 rather than the source's 10.0.6. |

The project diff consists of a BOM removal, combining two `ItemGroup`s,
and that one package override version change. Its target remains `net10.0`,
`IsPackable=false`, and its NUKE root/script directories remain `..`.
`dotnet restore build/_build.csproj --force-evaluate --nologo` completed
successfully in an isolated Core worktree at `34de0aa24bb785cd0b0ed4d5a237efb19dda2122`.
`dotnet msbuild build/_build.csproj -getProperty:UseArtifactsOutput
-getProperty:DirectoryBuildPropsPath -getProperty:DirectoryBuildTargetsPath
-getProperty:TargetFramework -getProperty:NuGetAuditMode` reported `false`,
the two local `build/Directory.Build.*` paths, `net10.0`, and `all`.
`-getItem:PackageReference` reported the three package versions above.
The isolated worktree had no tracked changes after restore. The draft import's
[exact-head full CI](https://github.com/elsa-workflows/elsa-core/actions/runs/36142805569)
also ran the central NUKE build on Linux.

This establishes representation of the NUKE tool inputs, not parity of the
different `build/Build.cs` pack/version behavior, Windows build execution, or
publisher ownership. Those remain separate ledger rows and release gates.
No active build, dependency, package or workflow file changes here.

Reproduce the source comparison with `git ls-tree` on the three active paths
and their `doc/integration-program/legacy/extensions/` `.source` counterparts
in the checked-out import draft. `diff -u` of the two `_build.csproj` files
shows the bounded differences above; `cmp` returns zero for the props and
targets files.
