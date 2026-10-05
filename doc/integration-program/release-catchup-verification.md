# Release and upstream consolidation catch-up

This checkpoint implements [Core #8623](https://github.com/elsa-workflows/elsa-core/issues/8623) under [program #8194](https://github.com/elsa-workflows/elsa-core/issues/8194). The original import is already merged through #8409. This catch-up carries subsequent release/source changes into the established Core layout; publisher cutover and the rest of the program remain separate work.

## History and source accounting

- Core's missing release ancestry is reconciled with reviewed equivalent changes as recorded in [the Core report](../reports/core-release-reconciliation-2026-10-06.md).
- [Extensions #285](https://github.com/elsa-workflows/elsa-extensions/pull/285) reconciles source release history at `b76f8fd216fdc529d41fbf39fd712d3fcd4e80fd`. Its accepted main merge is `70ea3018da0075c5590d4f537b7aaa74a5965527`; both are preserved in Core history. Every one of the 47 changed source paths is accounted in [the Extensions receipt](extensions-catchup-dispositions.json).
- [Studio #1061](https://github.com/elsa-workflows/elsa-studio/pull/1061) reconciles source release history at `dea56950ab7e269d13d008fd61b2ebd10454843c`, preserved in Core history. Every one of the 352 changed source paths is accounted in [the Studio receipt](studio-catchup-dispositions.json).
- [The release ref audit](release-catchup-refs.json) records all 35 advertised matching release branches and tags across the three repositories, including matching prerelease tags. All are ancestors of Core candidate `c304c05f45f263587cefd9e7dca6fb7a6a06ba58`. Remote identities are used because imported repositories can share tag names.

Receipts distinguish imported fixes from source-identical files, newer Core implementations, mapped project references, and inert historical configuration. Live Core build/version/publication settings remain authoritative. No repository cutover, feed publication, deployment, or archival is included.

## Validation before hosted integration CI

| Scope | Checked commit | Result |
| --- | --- | --- |
| Core release reconciliation: identity diagnostic, tenant visibility, and tenant-scoped memory key/value tests | `04bc3414692977cf3b918077ccd511da18938061` source tree | 50 passed, 0 failed, 0 skipped |
| Mapped Extensions: Persistence.Dapper, Dapper, MongoDB, Secrets | `14c6bd6512eb707d29072877e9f17582e6b6769b` | 202 passed, 0 failed, 0 skipped, .NET 10 |
| Combined Core Studio: Core, Dashboard, Environments, ExternalAuthentication, Administration, Workflows | `c304c05f45f263587cefd9e7dca6fb7a6a06ba58` | 1,339 passed, 0 failed, 0 skipped, .NET 10 |
| Extensions source hosted Compile/Test/Pack | `b76f8fd216fdc529d41fbf39fd712d3fcd4e80fd` | [Run 37381631473](https://github.com/elsa-workflows/elsa-extensions/actions/runs/37381631473): 498 passed, 2 unrelated provider tests skipped |
| Studio source hosted build and full solution tests | `dea56950ab7e269d13d008fd61b2ebd10454843c` | [Run 37382436307](https://github.com/elsa-workflows/elsa-studio/actions/runs/37382436307): 1,902 passed, 0 failed, 0 skipped |

The mapped Extensions suites used real `postgres:16` and `mongo:7.0.24` containers; container startup errors are not silently accepted. The six combined Studio suite totals are 155, 108, 23, 490, 73, and 490 respectively. Legacy asset validation also passed for all 163 assets (113 represented, 50 retired). Independent review checked receipt path completeness and Git modes/blobs, the preserved source/release ancestry, compatibility decisions, and the final functional aggregate. `git diff --check` passed.

These are scoped evidence checkpoints, not a claim that final hosted Core CI passed. Current-head CI and final independent review are recorded on the integration PR linked from #8623. The previously accepted Studio environment-load retry follow-up remains [Studio #1070](https://github.com/elsa-workflows/elsa-studio/issues/1070).
