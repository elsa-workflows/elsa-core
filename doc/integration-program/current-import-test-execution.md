# Current imported-source affected-test execution

Program #8194; independent-package proof #8260. The [selection receipt](current-import-impact-2026-09-25.json) and [execution receipt](current-import-test-execution-2026-09-25.json) are pinned to clean draft history-import source `aefa5bfd4fe43ecd695f0953d179187ab00e3268`. This includes the reviewed cross-tenant Secrets test and retained-asset dispositions through #8473. It is not a final merged-head release or package-publishing receipt.

The current-source selector restored 344 solution projects and 345 project assets, identified 95 runnable test projects by evaluated `IsTestProject`, and selected one Slack-only versus 52 shared-Core `net10.0` test/TFM nodes. Its receipt records the source and input hashes and reports `testExecutionPerformed=false`. The 52 selected nodes were then run sequentially with source project references, `--no-restore`, `IsPackable=false` and `GeneratePackageOnBuild=false`. All 52 produced TRX files and returned exit code zero. Across 4,028 selected test cases, 4,027 passed, zero failed and one was skipped. The component project is **incomplete**, not passed: `DeleteWorkflow_Clustered` remains disabled because its existing multi-pod/event-driven fixture interferes with other tests. The other 51 project runs are fully passing. The runner checks individual TRX outcomes rather than trusting a zero process exit or aggregate counters as acceptance. No package was packed or published.

The test-source checkout was clean before and after execution. The stored receipts contain repository-relative project paths, aggregate counters, the one skip name/reason, and hashes of local logs/TRX files; raw runtime logs remain outside the repository. Selection receipt SHA-256: `5852fec57342783c59e262d82fa1c208fb898cb11a840bb90528ab2b2cfc12be`; runner SHA-256: `cdd83b3c63d56fe2af68b6bd8e2a9c1e8f2ba9d9c7955700fc6b23351a264a16`; execution receipt SHA-256: `abad7fe2500afbfed18afcadf224f7a2f37bfd5a4ae16458fabece30fce27385`. The reusable runner added with this evidence fails closed on missing TRX, test failure, source drift or an unexecuted case; the inherited skip therefore returns an incomplete result, not release acceptance.

To reproduce, run the tooling from the reviewed PR checkout and point it at a clean isolated checkout of the exact source commit. Keep receipts outside both repositories:

```sh
PYTHONDONTWRITEBYTECODE=1 python3 scripts/integration-program/current_import_impact.py \
  --root /path/to/aefa-source \
  --expected-head aefa5bfd4fe43ecd695f0953d179187ab00e3268 \
  --output /tmp/current-import-impact.json
PYTHONDONTWRITEBYTECODE=1 python3 scripts/integration-program/run_current_import_affected_tests.py \
  --root /path/to/aefa-source \
  --selection /tmp/current-import-impact.json \
  --output-dir /tmp/current-import-test-execution \
  --expected-head aefa5bfd4fe43ecd695f0953d179187ab00e3268
```

The runner exits `3` for incomplete correctness closure, including the known skip. A manual `workflow_dispatch` lane in [Current imported-source test impact](../../.github/workflows/current-import-impact.yml) is prepared to test the runner policy in normal and optimized Python, then rerun selection and the affected tests on one exact checkout, upload only sanitized JSON receipts, and use read-only repository permissions. The workflow is still on the draft import branch, so this is not a claim that the lane can be dispatched from the repository default branch yet. Its job is expected to fail while the inherited skip remains; a failed job with a complete receipt is evidence, not accepted closure. PRs run only the bounded selection job, so this manual lane does not add 52 project runs to every change.

These commands exercise source-mode `net10.0` tests; they do not prove package-mode restore, net8.0/net9.0 consumers, SourceLink, sole-publisher ownership, or safe feed publication. The separate artifact-only [Slack release-unit PR #8436](https://github.com/elsa-workflows/elsa-core/pull/8436) covers a bounded earlier package/consumer proof, and must be refreshed on the accepted import head. #8260 remains open until the final source, artifact, publisher and rollback gates are reviewed.
