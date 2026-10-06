# Core 3.8.x and 3.9.0 reconciliation

Scope: reconcile Core release history into main without reverting newer main behavior. Tracker: [#8623](https://github.com/elsa-workflows/elsa-core/issues/8623).

## Release ancestry

Main started at `91a1c07232e806436c5e759086bbb9b0aee92234`. The following releases are ancestors after the reconciliation:

| Core remote ref | Commit | Disposition |
| --- | --- | --- |
| `release/3.8.1` | `249d0f077f8a0b5e6cb1a42b02b49f2f636b5ab5` | One missing backport commit reconciled by merge `e333ee6c9645f4900d046028c6453992fb8bef2d`. |
| `release/3.8.2`, `release/3.8.4` | `33181ae3048f628f591a0155b5665a8e4d1bcea2` | Already ancestors; no merge needed. |
| `release/3.9.0` | `5d3582b6309a2fd8ea33ebdb8c39fb040bfdc514` | Nine missing commits reconciled by merge `290f797ebb09ab597e6fa351497efcc1e18ca560`. |
| Stable tag `3.8.0` | `8191ae30554ea001b38bb44902dd90dc98c7a106` | Already an ancestor; peeled annotated tag commit. |
| Stable tags `3.8.1`, `3.8.2`, `3.8.4` | `33181ae3048f628f591a0155b5665a8e4d1bcea2` | Already ancestors. |
| Stable tag `3.9.0` | `6436609a1a3874d3fea7ccf690897792f5a1f702` | Already an ancestor. |

Stable tag identities were checked against `git ls-remote --tags origin '3.8.*' '3.9.0'` on 2026-10-06. The Core remote does not advertise a stable `3.8.3` tag; a foreign local tag of that name is excluded. Each commit above was verified with `git merge-base --is-ancestor` against the reconciliation head.

## Reviewed merge resolutions

All seven non-merge commits unique to the 3.9.0 release branch have matching stable patch IDs in main:

| Release commit | Equivalent main commit |
| --- | --- |
| `91f32dafe` | `919fecb3e` |
| `257bebc88` | `2fbeaaa9c` |
| `c9029051d` | `0ac8518af` |
| `1860081f9` | `ce10a3392` |
| `b98590a69` | `43c57da51` |
| `093dc1972` | `082b3ed1b` |
| `4f7fd42e3` | `c9db273a2` |

The other two missing commits, `7876e41d9` and `5d3582b63`, are release PR merge wrappers. `IdentityBootstrapDiagnostic.cs` is identical between the release tip and main, including the scoped 401/403 explanation, alternate identity provider caveat, and tenant-activation bootstrap timing.

Conflicts in `src/modules/Elsa.Identity/README.md` and `doc/migrations/authorization-model.md` retained main's generated role IDs and tenant-owned bootstrap behavior introduced by `c34117e52` and refined by `c09f38aff`, `efc508323`, `e3b0db7d5`, and `4d124da89`. Release 3.9's role-name-derived ID collision warning describes the older implementation; applying it as current main behavior would regress the documentation. Main also retains its MongoDB name-index caveats and 3.10 migration guidance.

The 3.8.1 backport restores ambient tenant stamping, visibility-filtered reads, and tenant-scoped deletes. Main already restored these in [#8434](https://github.com/elsa-workflows/elsa-core/pull/8434), merge `6a323ccc8be6396a817d0055f3b55b8be741fbc1`. Reviewed conflicts retained that implementation's additional `CanReplaceOwnedRow` ownership guard, `TryDeleteAsync`, existing-row tenant preservation, expanded tenant tests, and shared `TestTenantAccessor`. `MemoryStore.Sync` matches the backport. No release fix was discarded.

Both reconciliation merges have tree `5998b0f7277e15df47dc2e912a2b9fe7e5ae728d`, identical to the starting main tree. This establishes that source, versions, publication settings, and existing tests are unchanged by the ancestry reconciliation. This report is the only subsequent file addition.

## Verification

Targeted checks passed on the reconciled source tree under .NET 10:

| Project / fully qualified name filter | Passed | Failed / skipped |
| --- | --- | --- |
| `test/unit/Elsa.Identity.UnitTests/Elsa.Identity.UnitTests.csproj` / `IdentityBootstrapDiagnosticTests` | 5 | 0 / 0 |
| `test/unit/Elsa.Workflows.Runtime.UnitTests/Elsa.Workflows.Runtime.UnitTests.csproj` / `MemoryKeyValueStoreTenantIsolationTests` | 16 | 0 / 0 |
| `test/unit/Elsa.Common.UnitTests/Elsa.Common.UnitTests.csproj` / `TenantVisibilityTests` | 29 | 0 / 0 |

Each used `dotnet test <project> --filter FullyQualifiedName~<filter> -p:CollectCoverage=false -v minimal`, with normal restore in the isolated worktree. An initial identity attempt with `--no-restore` failed because the new worktree had no assets file; the normal restore/test retry above passed. Existing identity XML-documentation warnings appeared during the build; no build or test error remained.

`git diff --check` passed, all audit commit references resolve, and the merge resolution left no conflict markers in the reviewed files. Full solution qualification and independent final integration review remain with the root workroom lead; no full suite, push, publication, or GitHub merge was performed in this bounded slice.
