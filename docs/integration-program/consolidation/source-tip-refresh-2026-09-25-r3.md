# Studio review fixes and Core main refresh

Program #8194; story #8286; draft import #8409. [Studio PR #1067](https://github.com/elsa-workflows/elsa-studio/pull/1067) merged the source fixes for three import review comments after its exact-head build/test and 5/5 review. Studio main advanced from `f0eeb3c7428443b09512049fe890635fa4f7b427` to `099402226daba80e473a306bbd1243b8994465b8`. Its only two changed paths are the React sample's unused import and the OpenTelemetry test's comments on intentionally consumed async streams; the functional JSX and test assertions are unchanged. The focused Studio test project passed 35/35 locally before the comment-only review correction, and hosted build/test passed at the final PR head.

The [machine-readable receipt](source-tip-refresh-2026-09-25-r3.json) pins the two old/new source blobs and their exact mapped paths. Commit `fe0d6a3` copies the new Studio bytes into the consolidated tree. Merge commit `d4faa24` has `fe0d6a3` and Studio `0994022` as parents with an unchanged tree, preserving the upstream history. This follows the earlier [Extensions refresh](source-tip-refresh-2026-09-25-r2.md); it does not rewrite or squash that import history.

Core main also advanced through [#8463](https://github.com/elsa-workflows/elsa-core/pull/8463) to `24349a9580dc15be04ed96bb440e42b362f8006e`. Merge `9069fc2` brings that main commit into the draft. Its sole conflict was root `.gitignore`: the resolution retains both the previously reviewed Studio generated-output rules and the new Extensions-scoped developer/publishing rules, with authored source paths still visible. The merged Extensions policy document is carried without editing. The active Core Packages and wiki workflow blobs remain unchanged.

Run the [verifier](../../../scripts/integration-program/verify_import_source_tip_refresh_r3.py) and its fail-closed tests with:

```sh
PYTHONDONTWRITEBYTECODE=1 python3 scripts/integration-program/verify_import_source_tip_refresh_r3.py
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s scripts/integration-program -p test_import_source_tip_refresh_r3.py
PYTHONDONTWRITEBYTECODE=1 python3 -O -m unittest discover -s scripts/integration-program -p test_import_source_tip_refresh_r3.py
```

On the refreshed draft, the verifier passed; all four mutation tests passed in both Python modes; the earlier six-change and fourteen-change source-tip verifiers still passed. The relocated OpenTelemetry test project passed 35/35 on net10.0 (existing NuGet advisory warnings), and the 163-row retained-asset ledger validator passed. Git connectivity passed with only dangling local objects reported.

This source and main-branch refresh does not itself re-prove canonical build, package-mode consumption, Docker/Studio runtime behavior, Secrets compatibility, or the final retained-asset ledger. Re-run those at the new draft head before import acceptance. No package/feed publication or production change occurred.
