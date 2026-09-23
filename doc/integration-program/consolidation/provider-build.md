# Consolidated provider and sample build proof

Program #8194, build task #8287. This proof builds the history-preserving disposable rehearsal after applying reviewed provider integration changes. It does not import that history into Core or publish packages.

The first complete build with the Dapper and Mongo implementations failed with four errors, all in the Extensions workbench sample. The sample still registered legacy Secrets management against the now-canonical Core persistence projects. A focused sample correction replaces its legacy API and scripting project references with Core Secrets and Secrets JavaScript, configures persistence on Core's Secrets feature, and removes the legacy expiry task registration. The sample's existing `useSecrets = false` switch remains unchanged. When deliberately enabled, its options come from `Secrets`; the old `Secrets:Management` settings and database are not automatically migrated. Legacy API compatibility and canonical Studio behavior remain #8301's separate gate.

The corrected sample built successfully, and the subsequent complete `Consolidated.sln` build succeeded under each project's declared frameworks. The MySQL provider's net8/net9 restriction is retained; this command does not force the entire solution to net10.

| Execution | Errors | Warnings | Elapsed |
| --- | ---: | ---: | --- |
| Initial complete diagnostic | 4 | 1,915 | 15m 02s |
| Corrected workbench sample | 0 | 236 | 24s |
| Complete incremental build after correction | 0 | 1,561 | 1m 47s |

The warning counts differ because the final build reuses outputs from the diagnostic; they do not demonstrate that warnings were fixed. Warnings include missing SourceLink in the intentionally remote-free rehearsal, nullable/analyzer findings, dependency constraints and package security advisories. In particular, successful compilation does not establish that an EF dependency combination is supported at runtime. Package compatibility, security assessment before release, and complete test execution remain open.

## Tests previously blocked by restore

After the complete build, the five projects that could not restore in the historical clean-source CI run were tested against this mapped graph using the existing build outputs (`--no-build --no-restore`, net10). Logging passed 2 tests, LDAP 41, MQTT 57 and Quartz 7: **107 passed, 0 failed**. Azure Service Bus discovered its single existing skipped test; no provider behavior was exercised by that project. The receipt retains exact commands, test outcomes and logs. This establishes local execution after the mapped-source build; fresh CI and the complete dependency closure remain required.

## Reproduce

Follow [build preparation](build-preparation.md) to create the exact prepared baseline. Apply, in order, the [Dapper patch](../../../scripts/integration-program/consolidated-build/dapper-atomic-updates.patch), the Mongo patch from [PR #8304](https://github.com/elsa-workflows/elsa-core/pull/8304) at the artifact commit recorded in the receipt, and [the workbench correction](../../../scripts/integration-program/consolidated-build/workbench-canonical-secrets.patch). Use `git apply --check` before each application. The [evidence receipt](provider-build-evidence.json) records all three original source commits, the preparation baseline, artifact revision, changed-source hashes and compressed command logs.

```sh
dotnet build Consolidated.sln -p:UseProjectReferences=true \
  -p:IsPackable=false -p:GeneratePackageOnBuild=false -m:1
```

Raw command output is retained as reproducibly compressed logs under [provider-build-logs](provider-build-logs/); compare the uncompressed and compressed SHA-256 values with the receipt. The historical failed run remains evidence rather than being replaced by the successful rerun.

The upstream ledger was refreshed through authenticated, paginated GitHub REST reads: Extensions main remains `33fa0bfd28c7585240e3d4f665058c067b17e287` with 17 open PRs; Studio main remains `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` with 14. These contributor branches remain owned by their original PRs. A local Studio checkout at another commit is not the upstream main or an automatically selected import source.

Do not push the synthetic rehearsal history. The real import still requires original ancestry, package/activity identity and compatibility verification, current Core integration, final-layout debugging, independent package-consumer evidence, and normal reviewed merges. Package publication, production cutover and repository archival remain reserved approvals.
