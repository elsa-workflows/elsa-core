# Release and upstream consolidation catch-up

This checkpoint implements [Core #8623](https://github.com/elsa-workflows/elsa-core/issues/8623) under [program #8194](https://github.com/elsa-workflows/elsa-core/issues/8194). The original import is already merged through #8409. This catch-up carries subsequent release/source changes into the established Core layout; publisher cutover and the rest of the program remain separate work.

## History and source accounting

- Core's missing release ancestry is reconciled with reviewed equivalent changes as recorded in [the Core report](../reports/core-release-reconciliation-2026-10-06.md).
- [Extensions #285](https://github.com/elsa-workflows/elsa-extensions/pull/285) reconciles source release history at `b76f8fd216fdc529d41fbf39fd712d3fcd4e80fd`. Its accepted main merge is `70ea3018da0075c5590d4f537b7aaa74a5965527`; both are preserved in Core history. The persistence review follow-up [Extensions #286](https://github.com/elsa-workflows/elsa-extensions/pull/286) at `8626e81f4a780be7d5fde87a1ae8b15cc6915934` is also mapped. Every one of the 48 changed source paths is accounted in [the Extensions receipt](extensions-catchup-dispositions.json).
- [Studio #1061](https://github.com/elsa-workflows/elsa-studio/pull/1061) reconciles source release history at `2cdfcc525945e99a2ab2c1a02fa3053a0e8dce20`. Its accepted main merge is `e712b5e6df735ed132a1bf20ea77f35a5a1b5759`; both are preserved in Core history. The follow-up [Studio #1125](https://github.com/elsa-workflows/elsa-studio/pull/1125) at `734da587eb3aca7b935375cce60bd536b6afdc6f` was accepted as main merge `3d028a4e9a3267b291f3d43d8064b5c81a6f2759`; this history is also preserved. Every one of the 353 changed source paths is accounted in [the Studio receipt](studio-catchup-dispositions.json).
- [The release ref audit](release-catchup-refs.json) records all 35 advertised matching release branches and tags across the three repositories, including matching prerelease tags. All are ancestors of Core candidate `c304c05f45f263587cefd9e7dca6fb7a6a06ba58`. Remote identities are used because imported repositories can share tag names.

Receipts distinguish imported fixes from source-identical files, newer Core implementations, mapped project references, and inert historical configuration. Live Core build/version/publication settings remain authoritative. No repository cutover, feed publication, deployment, or archival is included.

## Pinned validation checkpoints

| Scope | Checked commit | Result |
| --- | --- | --- |
| Core release reconciliation: identity diagnostic, tenant visibility, and tenant-scoped memory key/value tests | `04bc3414692977cf3b918077ccd511da18938061` source tree | 50 passed, 0 failed, 0 skipped |
| Mapped Extensions: Persistence.Dapper, Dapper, MongoDB, Secrets | `14c6bd6512eb707d29072877e9f17582e6b6769b` | 202 passed, 0 failed, 0 skipped, .NET 10 |
| Combined Core Studio: Core, Dashboard, Environments, ExternalAuthentication, Administration, Workflows | `c304c05f45f263587cefd9e7dca6fb7a6a06ba58` | 1,339 passed, 0 failed, 0 skipped, .NET 10 |
| Extensions source hosted Compile/Test/Pack | `b76f8fd216fdc529d41fbf39fd712d3fcd4e80fd` | [Run 37381631473](https://github.com/elsa-workflows/elsa-extensions/actions/runs/37381631473): 498 passed, 2 unrelated provider tests skipped |
| Studio source hosted build and full solution tests | `dea56950ab7e269d13d008fd61b2ebd10454843c` | [Run 37382436307](https://github.com/elsa-workflows/elsa-studio/actions/runs/37382436307): 1,902 passed, 0 failed, 0 skipped |

The subsequent Studio review found a logout form that ignored a hosting prefix. Studio source `2cdfcc525945e99a2ab2c1a02fa3053a0e8dce20` reuses the existing Core implementation; its rendered regression failed for two subpaths before the fix and all 16 focused sign-out cases passed after it. Core production already handled the prefix. Stronger Core rendered tests passed all 17 focused sign-out cases at `88c241591fdb61766bc5e3ce5b6dadb082aec67d`. The refreshed full source CI at `2cdfcc525945e99a2ab2c1a02fa3053a0e8dce20` subsequently passed with 1,908 tests, zero skipped and zero build errors ([run 37384476983](https://github.com/elsa-workflows/elsa-studio/actions/runs/37384476983)). Independent exact-head approval and Greptile 5/5 preceded the source merge. These later results are separate from the earlier full-suite checkpoints above.

## Review follow-ups

The final persistence slice preserves existing tenant ownership during Dapper saves, treats prefix metacharacters literally (including PostgreSQL backslashes), and compares SQLite date/time offsets as instants. A primary-key-protected insert and narrow duplicate retry prevent reassignment during concurrent creation; updates change only Value within the ambient tenant. The ownership regression produced three failures and one control pass before the fix, then all 26 SQLite/PostgreSQL cases passed. SQLite sub-millisecond timestamp ties remain excluded from the exclusive liveness cutoff until a later scan.

At Extensions source `8626e81f4a780be7d5fde87a1ae8b15cc6915934`, the complete persistence Dapper suite passed 98 tests and generic Dapper passed 20. At mapped Core `e6dd8a1617d3f54f683af9c6ff810aa9d3bf213b`, the respective suites passed 130 and 21. All had zero failures/skips. Independent review parsed the raw TRX results.

Mongo regression coverage now distinguishes legacy JsonObject documents with envelope-like scalar/array fields from genuine envelopes, including polymorphic dispatch. The added scalar and array regressions failed before their fixes. Final converter tests passed 21/21 on both trees; the complete unchanged Mongo slice passed 72 source and 89 Core tests, zero failures/skips. The public VariableSerializer constructor change is documented in [the migration note](../migrations/mongodb-variable-serializer.md).

Studio follow-ups prevent foreign feature names from taking the Elsa short-name fallback and cap the 600-pixel drawer to the viewport. Source `734da587eb3aca7b935375cce60bd536b6afdc6f` passed the complete hosted build and 1,911 tests with zero failures/skips across 18 target-framework results ([run 37386031336](https://github.com/elsa-workflows/elsa-studio/actions/runs/37386031336)). Independent exact-head approval and Greptile 5/5 preceded merge. Final Core CI separately qualifies these mapped changes.

The mapped Extensions suites used real `postgres:16` and `mongo:7.0.24` containers; container startup errors are not silently accepted. The six combined Studio suite totals are 155, 108, 23, 490, 73, and 490 respectively. Legacy asset validation also passed for all 163 assets (113 represented, 50 retired). Independent review checked receipt path completeness and Git modes/blobs, the preserved source/release ancestry, compatibility decisions, and the final functional aggregate. `git diff --check` passed.

These are scoped evidence checkpoints, not a claim that final hosted Core CI passed. Current-head CI and final independent review are recorded on the integration PR linked from #8623. The previously accepted Studio environment-load retry follow-up remains [Studio #1070](https://github.com/elsa-workflows/elsa-studio/issues/1070).
