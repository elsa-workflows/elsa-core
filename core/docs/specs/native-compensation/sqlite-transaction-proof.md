# SQLite native commit transaction proof

Status: bounded provider experiment; the final retained-output run is pending. This report is not a provider qualification, a shipped compare-and-swap guarantee, or an E1/M1 pass.

## Question and result so far

This experiment asks whether a test-only adapter can make Elsa's existing `DefaultCommitStateHandler` commit its correctness-dependent writes through one EF Core SQLite connection and transaction, and whether a real zero-row final snapshot update then rolls earlier writes back. It also records the provider's native ambient-transaction behavior.

At source head `c02308c7e19e7358c78fe4dd16e2c2e2892ab826`, the focused four-case test run passed. The success and real conditional-conflict cases observed shared-transaction participation and rollback; the transaction-disabled case demonstrated partial writes; the ambient case stopped at EF Core's unsupported-provider warning. That run did not retain the passing test's numeric snapshot comparisons in its console log. The current candidate adds sanitized `ITestOutputHelper` comparisons and synchronous context cleanup on enlistment failure. It has not yet been rerun; numeric before/staged/after observations remain pending.

## Scope and method

The fixture in `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/SqliteCommitTransactionParticipation/Tests.cs` uses one synthetic enabled tenant and a serial local workflow. Its test-only context factory enlists Management and Runtime EF contexts in an externally owned SQLite connection and transaction. The normal `DefaultCommitStateHandler` pipeline performs bookmark persistence, activity and workflow execution-log writes, variable persistence, and workflow-instance persistence.

The source paths behind that inventory are `src/modules/Elsa.Workflows.Runtime/Services/DefaultCommitStateHandler.cs` (write ordering and post-commit notification flush), `src/modules/Elsa.Workflows.Runtime/Services/BookmarkPersister.cs`, `src/modules/Elsa.Workflows.Runtime/Services/StoreActivityExecutionLogSink.cs`, `src/modules/Elsa.Workflows.Runtime/Services/StoreWorkflowExecutionLogSink.cs`, `src/modules/Elsa.Workflows.Core/Services/VariablePersistenceManager.cs`, `src/modules/Elsa.Workflows.Core/VariableStorageDrivers/WorkflowInstanceStorageDriver.cs`, and `src/modules/Elsa.Persistence.EFCore/Modules/Management/WorkflowInstanceStore.cs` (serialized `Data` field). The native commit transaction implementation at `src/modules/Elsa.Persistence.EFCore/Modules/Runtime/EFCoreWorkflowCommitTransaction.cs` uses `TransactionScope`; this experiment does not change it.

For the stale-run case, the fixture first writes a valid newer snapshot B through `IWorkflowInstanceManager` and a separate native EF context. It retains the original snapshot A in the execution fixture, then stages the stale run's bookmark and log writes. A test-only `DbCommandInterceptor` adds an expected-`Data` predicate to the actual final `WorkflowInstances` update, preserving the provider command and its parameters. SQLite executes that update and returns zero rows; the resulting concurrency exception propagates through the shared transaction. Fresh reads use independent contexts/connections. The interceptor is an experiment-only way to provoke and observe a conditional conflict; it does not establish that the shipped store has this predicate.

The built-in workflow-instance variable is stored in the `Variables` dictionary nested in the owning activity execution context's serialized properties. The fixture checks that persisted representation and reloads it independently. This covers the built-in storage driver's inline representation for the synthetic variable; it does not exercise `IVariableManager` definition materialization or external variable-storage drivers.

The candidate's passing-test output is designed to include only snapshot SHA-256 hashes, row counts, transaction-presence flags, callback method names, expected/attempted preimage hashes, and the conditional update's returned-row result. It does not print serialized snapshot or variable values. The final retained numeric output is pending the reviewed TRX-logger run.

## Cases and boundaries

- Shared-transaction success: the conditional update accepts A; bookmark replacement, workflow/activity logs, inline variable state, and snapshot change are read back after commit.
- Shared-transaction conflict: B is committed by the native manager before the stale run. The final predicate expects A, returns zero rows, and the transaction rolls back staged bookmark/log work. Independent reads must still show B and the old bookmarks/log counts.
- Transaction disabled: the same native calls and test interceptor run without the shared-transaction adapter. The final update returns zero rows while earlier bookmark/log commands remain committed. This is a negative control demonstrating why transaction participation matters; it is not a production configuration recommendation.
- Native ambient transaction: the existing SQLite provider does not support EF Core ambient transactions. The default warning behavior throws during the initial native commit. The test does not suppress that warning and verifies independent database counts at the failure boundary.

No callback-bearing workflow work was exercised. The handler flushes deferred post-commit notifications after the transaction, so callback/notification aftermath is outside this commit-pipeline proof. There is no process-kill, unknown-acknowledgment, simultaneous two-process race, Studio, E2, or E3 evidence. B is a controlled manager write before stale resume. The fixture's tenant filter and persisted tenant readback prove only that this synthetic fixture was tenant-configured; they do not prove cross-tenant authorization or a tenant-bound compare-and-swap predicate. The SQL assertion that looks for `TenantId` is not itself proof that the term occurs in the `WHERE` predicate.

The experiment is bounded to SQLite and its local provider semantics. It does not cover other providers, external storage drivers, production CAS, owner/epoch fencing, migrations, or public transaction APIs. The smallest next design seam is the native connection/transaction participation required by issue #7973, not a new general transaction API. The reviewed guarded-snapshot contract is available at [the merged contract snapshot](https://github.com/elsa-workflows/elsa-core/blob/3d78ba3f20cf26039776cb91eb23a963caeeb723/core/docs/specs/native-compensation/guarded-snapshot.md); this branch predates that documentation merge and intentionally does not duplicate it.

## Verification history

All attempts are preserved under `/tmp/elsa-8936-sqlite-proof-test-*.log`. Earlier compile and fixture failures are retained rather than replaced by the passing run:

| Candidate | Log | Outcome |
| --- | --- | --- |
| `3d199ef` | `/tmp/elsa-8936-sqlite-proof-test-3d199ef.log` | Compile failed: SQLite transaction type mismatch, invalid SQLite options receiver, workflow/graph type mismatch, and missing `RunWorkflowOptions` namespace. |
| `a5b2fb6` | `/tmp/elsa-8936-sqlite-proof-test-a5b2fb6.log` | Compile failed: `RunWorkflowOptions` namespace still incorrect. |
| `5ada913` | `/tmp/elsa-8936-sqlite-proof-test-5ada913.log` | 0 passed, 3 failed: test schema was created after registry population queried `WorkflowDefinitions`. |
| `22730fe` | `/tmp/elsa-8936-sqlite-proof-test-22730fe.log` | 0 passed, 3 failed: tenant accessor replacement was overridden, leaving persisted tenant IDs empty. |
| `00075f6` | `/tmp/elsa-8936-sqlite-proof-test-00075f6.log` | 0 passed, 3 failed: repeated interceptor callback injection duplicated parameters, and the initial variable had only a declaration rather than a persisted assignment. |
| `d355477` | `/tmp/elsa-8936-sqlite-proof-test-d355477.log` | 2 passed, 1 failed; the two passing negatives were provisional because the fixture wrote/read a root property bag instead of the actual nested activity-context variable. |
| `bf0ac63` | `/tmp/elsa-8936-sqlite-proof-test-bf0ac63.log` | 3 passed, 0 failed after correcting the nested variable representation; no ambient test or retained numeric output yet. |
| `c02308c` | `/tmp/elsa-8936-sqlite-proof-test-c02308c.log` | 4 passed, 0 failed in the focused filter; passing-test numeric output was not retained by the console logger. |

The prior successful run used:

```text
/Users/sipke/.local/share/dotnet-build-slots/bin/dotnet test core/test/integration/Elsa.Workflows.IntegrationTests/Elsa.Workflows.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~SqliteCommitTransactionParticipation -m:1 -nologo
```

The current source candidate adds the output needed to retain those measurements through the TRX logger and fixes synchronous context disposal if transaction enlistment throws. The root-reviewed final focused run with a TRX logger is pending; no final-candidate measurements are asserted here. No broad integration suite or full native workflow execution is claimed.
