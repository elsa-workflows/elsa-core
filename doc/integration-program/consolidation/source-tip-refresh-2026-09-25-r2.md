# Extensions persistence source-tip refresh

Program #8194; story #8286; draft import PR #8409. After the [first source-tip refresh](source-tip-refresh-2026-09-25.md), Extensions main advanced from `1c134e00f50a7ac36ea61e6c442dc3c5148db049` to `9361c80e2ea56ccf71118fb53d6fd8f3d0fbbbf0` through PR #231. Studio remained at `f0eeb3c7428443b09512049fe890635fa4f7b427`. This second refresh stays on the same history-bearing import branch; it does not replace or squash the original import.

The [machine-readable receipt](source-tip-refresh-2026-09-25-r2.json) records all 14 changed Extensions paths, their old and new source blobs and modes, and their mapped Core paths. Commit `9d3e48f` maps the exact new upstream bytes, including four added tests. Merge commit `d1d785f` has parents `9d3e48f`, `9361c80`, and `f0eeb3c`; its tree matches its first parent. The changed Extensions solution remains inert `.source` evidence. No active Core publishing workflow changed.

The imported Dapper project file had already received reviewed consolidated project-reference rewrites. At the history join it contains the **exact** new upstream file. Commit `4f953b2` then reapplies those consolidated references while carrying upstream's new `Elsa.Dapper.UnitTests` friend-assembly entry. Commit `c83aa78` points the new test project at the consolidated source and adds it to canonical `Elsa.sln`, so NUKE can select it. The [verifier](../../../scripts/integration-program/verify_import_source_tip_refresh_r2.py) checks the exact upstream diff, all merge parents and ancestry, source-to-mapped Git blobs/modes, the two bounded project transformations, canonical solution membership, and unchanged active Core publication workflows. Its tests reject altered source blobs, relocation, and unreviewed transformations.

Local net10.0 test runs passed 10/10 new Dapper tests and 20/20 MongoDB tests. Restore reported existing package-advisory warnings; those warnings are not release approval. Run the verifier and targeted tests with:

```sh
python3 scripts/integration-program/verify_import_source_tip_refresh_r2.py
dotnet test test/extensions/modules/persistence/Elsa.Dapper.UnitTests/Elsa.Dapper.UnitTests.csproj --framework net10.0
dotnet test test/extensions/modules/persistence/Elsa.MongoDb.UnitTests/Elsa.MongoDb.UnitTests.csproj --framework net10.0
```

The earlier E96 and first-refresh receipts remain pinned historical evidence, not claims about this later tip. The complete canonical NUKE run, mapped package proof at the new import head, final browser/identity replay, legacy asset dispositions, and Secrets upgrade/custody approval remain separate gates. No package was published or production environment changed.
