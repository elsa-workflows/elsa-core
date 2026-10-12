# Developing Elsa Templates

Elsa Templates provides the independent `Elsa.Templates` package and the `elsa-server`, `elsa-studio` and `elsa-combined` .NET project/solution commands. Current starters target net10.0 and consume released Elsa 3.9.0 and CShells 0.0.28. Available options and usage examples are in the [product README](../README.md).

Run these commands from the repository root:

```sh
dotnet restore Elsa.Templates.slnf
dotnet build Elsa.Templates.slnf -c Release --no-restore
dotnet build templates/Elsa.Templates.slnx -c Release --no-restore
dotnet test templates/test/Elsa.Templates.Tests/Elsa.Templates.Tests.csproj -c Release --no-build
dotnet pack templates/src/Elsa.Templates/Elsa.Templates.csproj -c Release
```

The pack output remains `templates/artifacts/package/release/Elsa.Templates.3.9.0.nupkg`. Install locally into an isolated template hive:

```sh
hive=$(mktemp -d)
dotnet new install templates/artifacts/package/release/Elsa.Templates.3.9.0.nupkg --debug:custom-hive "$hive"
dotnet new elsa-server -n MyElsaServer -o /tmp/MyElsaServer --debug:custom-hive "$hive"
rm -rf "$hive"
```

The root filter selects `templates/Elsa.Templates.slnx`, whose only build projects are the template pack and smoke tests. Embedded starter project files are package data, and generated copies build in isolated system-temp workspaces. Templates stays outside root `Elsa.sln` project membership and Core NUKE packing, so a shared Core package version does not become the Templates release version. Root solution navigation links open this separate product.

The [current inventory](../../scripts/solution/solution-groups.json) explicitly accounts for all nine tracked manifests: two build projects, six embedded starter projects and one retained standalone root host. The donor's lowercase `elsa-templates.slnx` points at a missing `elsa_templates.csproj`; it is retained historical input, not a supported build entrypoint. Use the case-sensitive `Elsa.Templates.slnx` above. No legacy files are deleted or silently repaired.

Run inexpensive provenance/configuration checks without .NET:

```sh
python3 scripts/integration-program/verify_templates_source_import.py
python3 -m unittest discover -s scripts/integration-program -p 'test_verify_templates_source_import.py' -v
python3 -m unittest discover -s templates/test -p 'validate_packages_workflow.py' -v
python3 scripts/solution/solution_groups.py --check
ruby scripts/integration-program/test_dependabot_discovery.rb
```

The [Templates source workflow](../../.github/workflows/templates-source-validation.yml) supplies native proof for both product and root-filter commands, effective scoped MSBuild/NuGet policy, and actual pack/install/generate/build across all 20 existing smoke cases. It requires every case to execute and pass. Every generated Studio server and WASM entrypoint asserts custom-element registration before building against the released API. This extends the retained PR #9 regression across standalone and combined hosting; it does not establish a runtime fix for donor issue #3.

Static checks do not establish restore or runtime success. Before source acceptance, require exact-head hosted CI and independent review. The import's local work deliberately defers .NET execution on the overloaded shared host. Supported source options exclude MySQL and preview-only Studio modules; no additional provider, runtime hosting, publisher or release guarantee is inferred.

Read [Configure Identity](../README.md#configure-identity) for deployment signing-key requirements and [Verification](../README.md#verification) for browser editor limitations and the #8741 follow-up scope.

The [import record](../../docs/integration-program/templates-source-import.md) explains history, attribution, immutable provenance and the separate [#8741](https://github.com/elsa-workflows/elsa-core/issues/8741) / [#8725](https://github.com/elsa-workflows/elsa-core/issues/8725) release/publisher gates. Retained [roadmap](roadmap.md) and [tasks](tasks.md) files describe donor planning, not new release authority.
