# Agents Studio source and package reference modes

Program #8194; build integration #8286; paired source development #8215.

The archived Extensions `.build/ElsaStudio.ProjectReferences.targets` at source
commit `33fa0bfd28c7585240e3d4f665058c067b17e287` (blob
`b8616ef3ba85243159438aaaea45c5ce8b4d1907`) injects a
Studio Workflows project reference when `UseProjectReferences=true`. Its old
path points outside the Extensions repository. The imported
`src/extensions/agents/Elsa.Studio.Agents/Elsa.Studio.Agents.csproj` carries the
mapped project reference itself, so that archived target is not an active
MSBuild input. The imported project initially used the same mapped project
reference in both conditional branches. Package mode therefore still pulled
Studio source even though the pinned Extensions project originally used the
public `Elsa.Studio.Workflows` package in that branch.

The corrected project now has two evaluated modes:

| Mode | Studio Workflows dependency | Other source dependency |
| --- | --- | --- |
| `UseProjectReferences=true` | `src/studio/modules/Elsa.Studio.Workflows/Elsa.Studio.Workflows.csproj` | `Elsa.Agents.Models` project |
| `UseProjectReferences=false` | `Elsa.Studio.Workflows` package, centrally versioned by `ElsaStudioVersion` | `Elsa.Agents.Models` project |

This preserves the public package ID while allowing the backend/Studio source
pair to compile together. The Agents Models project remains a source reference
in both modes; this check does not establish a fully independent release unit
for all Agents packages.

On draft import base `e2711ab0def8be84ef8cf1d7c17f82e153cacc36` with
the one-line project correction in this change, `dotnet msbuild -getProperty:UseProjectReferences
-getItem:PackageReference -getItem:ProjectReference` showed the two graphs above.
With an empty isolated package cache, the active root `NuGet.Config` restored
`Elsa.Studio.Workflows` 3.8.0 from NuGet.org for net8.0, net9.0 and net10.0.
The explicit package-mode Release build passed all three target frameworks with
zero errors and 18 warnings (existing NU1902, nullable and MudBlazor analyzer
warnings). The source-mode Release build also passed all three targets with zero
errors and 56 warnings. These are build and reference-graph checks, not pack,
clean-consumer, browser, or publisher proofs. No package/feed publication ran.
The existing `Elsa.Studio.Agents.Tests` project passed 6/6 tests in Release
on net10.0 with source references.

Reproduce from the repository root, with `/path/to/empty-cache` replaced by a
new empty absolute directory:

```sh
dotnet msbuild src/extensions/agents/Elsa.Studio.Agents/Elsa.Studio.Agents.csproj \
  -getProperty:UseProjectReferences -getItem:PackageReference \
  -getItem:ProjectReference -p:UseProjectReferences=false
NUGET_PACKAGES=/path/to/empty-cache dotnet restore \
  src/extensions/agents/Elsa.Studio.Agents/Elsa.Studio.Agents.csproj \
  --configfile NuGet.Config --force-evaluate --no-cache \
  -p:UseProjectReferences=false -p:ElsaVersion=3.8.4 \
  -p:ElsaStudioVersion=3.8.0
NUGET_PACKAGES=/path/to/empty-cache dotnet build \
  src/extensions/agents/Elsa.Studio.Agents/Elsa.Studio.Agents.csproj \
  --configuration Release --no-restore \
  -p:UseProjectReferences=false -p:ElsaVersion=3.8.4 \
  -p:ElsaStudioVersion=3.8.0
dotnet build src/extensions/agents/Elsa.Studio.Agents/Elsa.Studio.Agents.csproj \
  --configuration Release -p:UseProjectReferences=true
```

The legacy target stays preserved as an inert `.source` file for exact import
provenance. After reviewed [PR #8482](https://github.com/elsa-workflows/elsa-core/pull/8482)
merged as `3ee23cd2ac36f4ad3daa8731c6126175d98ae9b6`, its asset-ledger row
records retirement from the active tree. The final history import and
publisher cutover remain separate gates.
