# Studio timer-test source refresh

Program #8194; story #8286; draft history import #8409. The latest full import CI at
`e54e2f2` exposed a test teardown race in the imported Studio
`WorkflowInstanceDesignerDisconnectRefreshTests`: the timer callback could still
wait on a `ManualResetEventSlim` after the test disposed it. Studio
[PR #1069](https://github.com/elsa-workflows/elsa-studio/pull/1069) changed only
that test. Its exact head `7a381f4` passed the focused test 20 consecutive
times locally, the full Studio Workflows suite 447/447 in both Debug and
Release, hosted Build and test and CodeQL, and Greptile 5/5 with no blocking
finding. It merged into Studio main as `5b34ec327caffd132e18bfd88bfc9e862dc35c43`.

The [machine-readable receipt](source-tip-refresh-2026-09-25-r4.json) pins the
one changed upstream path and its old/new Git blobs and modes. Import commit
`286e5df` maps those reviewed bytes into `src/studio/` without another source
transformation. Merge commit `6dec73e` has the mapped commit and Studio
`5b34ec3` as parents with an unchanged tree. This preserves the prior Core,
Extensions and Studio histories instead of rewriting the draft import.

Run the [verifier](../../../scripts/integration-program/verify_import_source_tip_refresh_r4.py)
and its mutation tests with:

```sh
PYTHONDONTWRITEBYTECODE=1 python3 scripts/integration-program/verify_import_source_tip_refresh_r4.py
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s scripts/integration-program -p test_import_source_tip_refresh_r4.py
PYTHONDONTWRITEBYTECODE=1 python3 -O -m unittest discover -s scripts/integration-program -p test_import_source_tip_refresh_r4.py
```

The verifier checks the exact upstream diff, mapped path and bytes, merge
parents and ancestry, and unchanged active Core Packages and wiki workflows.
It does not claim full import CI passed; that run must be repeated at the new
head. The Studio merge used `[skip ci]`, and no Studio Packages workflow run
was created for that merge. No package/feed publication or production change
occurred.
