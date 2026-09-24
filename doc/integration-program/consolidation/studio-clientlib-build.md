# Studio browser asset build in the consolidated source layout

`scripts/integration-program/build_studio_clientlibs.sh` restores the mapped
Designer project, checks BPMN types against its pinned `Bpmn.Model` schema,
runs the Designer's TypeScript/Vitest suite, builds the Designer and
DomInterop ClientLibs, and requires their six browser entry assets. It
requires Node 22 and the normal .NET SDK/restore access. The script fails
when Studio source has not been imported.

The accompanying `studio-bpmn-generator-layout.patch` changes the pinned
Studio generator to accept the standalone Studio root or the mapped
`src/studio` root. It selects the only candidate `Directory.Packages.props`
containing `BpmnModelVersion` and rejects missing or ambiguous roots. The
patch is an import-time source adjustment; it is not applied to current
elsa-core `main` until the Studio history import lands.

On 2026-09-24, the script passed on an isolated mapped source clone pinned to
Core `95a658b96107ad4dbb280a13972479af74bc6a30`, Extensions
`33fa0bfd28c7585240e3d4f665058c067b17e287`, and Studio
`9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`, with that patch applied.
Node was `22.22.1`; `npm run check:generated` matched `Bpmn.Model 0.2.0`,
Designer passed 253 tests and type checking, and both webpack builds passed.
The six expected assets matched the earlier bounded Workbench/Studio runtime
receipt in `doc/integration-program/secrets/workbench-studio-runtime/receipt.json`.
The script used the pinned Studio PR's `npm install --force` behavior while
disabling lockfile creation, since that source pin contains no npm lockfiles.

This is a local mapped-source proof. The history-bearing import and a
required CI build on the imported Studio source remain open under #8286 and
#8287. The package install reported four Designer dependency advisories
(three moderate, one high); the .NET restore reported `NU1902` for
`Microsoft.Build.Tasks.Git 10.0.103`. Those warnings did not fail this proof
and require separate dependency review before release.
