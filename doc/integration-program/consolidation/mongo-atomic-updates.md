# MongoDB atomic workflow updates in consolidated source

Program #8194, story #8286, task #8294. This is a narrow integration patch for the disposable source-consolidation rehearsal. It is not an upstream import, release package, or published artifact. Apply it only after #8291's reviewed preparation, against the Core, Extensions, and Studio source pins recorded in the receipt.

## Behavior

`MongoWorkflowDefinitionStore.TryUpdateLatestAsync` reads the latest visible workflow row and its raw BSON snapshot in one MongoDB session transaction. It checks the expected-state callback, applies the update once, then conditionally replaces the exact full-document snapshot using simple collation. A published-to-draft transition conditionally clears the selected row's latest flag and inserts the new draft in the same transaction. Tenant visibility comes from the filter's existing tenant-aware read path; the selected tenant ID is retained, and an update cannot change the logical definition ID. The original one-argument store constructor remains available.

The transaction uses snapshot reads, primary reads, and majority writes. It obtains the client from the collection's owning database, so a custom collection cannot accidentally be paired with a different injected client. The tested deployment is a MongoDB replica set; a standalone server cannot silently fall back to non-transactional behavior. Only known aborted transaction conflicts are returned as `Conflict`. The `matchesExpected` and `update` callbacks execute once and their exceptions propagate even when they carry MongoDB's transient-transaction label. Commit exceptions propagate because their outcome may be ambiguous; the implementation does not retry the callback or the transaction.

## Pinned rehearsal inputs

The source baseline is the disposable rehearsal created by #8291, commit `926c91327efaecce286d50b31f19db376239f05e`, with original source commits Core `8e893e02c4ac089d526b0a0d294a8546f021d072`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, and Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`. The receipt records these pins, the source patch digest, and the hashes of each patched file.

`Testcontainers.MongoDb` resolves to `4.9.0` through `src/extensions/Directory.Packages.props` in that baseline (SHA-256 `750eceddbf4d573378a0eb52116b2c2a96c076e7048d812f5feb17c027dda6a6`), imported by `test/extensions/Directory.Packages.props`. The clean replay uses that version as-is; no temporary package-version edits or hidden restore overrides are part of the proof. The test fixture uses MongoDB `7.0.24` with replica set `rs1`; this run resolved `mongo:7.0.24` to digest `mongo@sha256:4c65244b50910461b9641a76131f84a2dcfd4da487f928298cea626b3842842c` on local linux/arm64 Docker.

## Verification

The proof applies the checked-in patch to a fresh detached worktree at the exact rehearsal baseline, performs a forced restore into an isolated NuGet package directory, checks the resolved Testcontainers version, then runs the focused replica-set tests. Full restore, package-resolution, test, and project-build logs are retained in [the evidence log directory](mongo-atomic-updates-logs/).

| Check | Result |
|---|---|
| Fresh patch replay at `926c91327efaecce286d50b31f19db376239f05e` | Passed; checked patch application and replay file hashes are in receipt |
| Forced restore with isolated package cache; `Testcontainers.MongoDb` | Passed; resolved version 4.9.0 |
| Mongo compare-and-swap replica-set tests, net10.0 | 9 passed, 0 failed, 0 skipped |
| Mongo persistence project, net8.0/net9.0/net10.0 | Passed, 0 errors, 50 disclosed warnings |

The replica-set tests cover same-row updates, two simultaneous writers released from a barrier after both read the same snapshot, stale metadata, callback exceptions carrying a transient label, tenant visibility and preservation, logical-definition identity, published-to-draft updates, and rollback when draft insertion fails. The two-writer case expects exactly one committed winner and one `Conflict` loser.

Evidence, exact source hashes, logs, package-resolution output, and limitations are recorded in [mongo-atomic-updates-evidence.json](mongo-atomic-updates-evidence.json). This rehearsal is not the released Elsa 3.8.4 compatibility proof, does not establish the final imported solution's complete build, and does not include package creation or publishing.

## Reproduce

Create a fresh disposable rehearsal from the #8291 preparation at the three commits listed in the receipt. From the prepared repository, apply the patch and run:

```sh
git apply --check /path/to/elsa-core/scripts/integration-program/consolidated-build/mongo-atomic-updates.patch
git apply /path/to/elsa-core/scripts/integration-program/consolidated-build/mongo-atomic-updates.patch
NUGET_PACKAGES=/tmp/elsa-mongo-proof-nuget dotnet restore test/extensions/modules/persistence/Elsa.MongoDb.UnitTests/Elsa.MongoDb.UnitTests.csproj --force --no-cache
dotnet list test/extensions/modules/persistence/Elsa.MongoDb.UnitTests/Elsa.MongoDb.UnitTests.csproj package --no-restore
dotnet test test/extensions/modules/persistence/Elsa.MongoDb.UnitTests/Elsa.MongoDb.UnitTests.csproj --no-restore -f net10.0 --filter FullyQualifiedName~MongoWorkflowDefinitionStoreCompareAndSwapTests
dotnet build src/extensions/persistence/Elsa.Persistence.MongoDb/Elsa.Persistence.MongoDb.csproj --no-restore
```

The test project requires Docker and starts its own replica-set container. Do not point it at a production MongoDB service. The logs in the receipt are from an isolated local development Docker runtime.

## Integration gate

Keep #8294 open until this patch is incorporated into the history-preserving import, the real consolidated solution builds on all declared target frameworks, and the final imported MongoDB tests pass. This artifact does not authorize package publication, source import, release, or a change to publisher workflows. Standalone MongoDB behavior, historical production databases, and the final imported layout remain outside this proof.
