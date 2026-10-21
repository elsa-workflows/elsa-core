# SQLite native commit transaction proof

Status: bounded provider experiment; the latest focused four-case run passed at source head `825e15dcb41977aa8d7025564f746a3298f4f134` using .NET SDK `10.0.300` and .NET runtime `10.0.8`. The measured test assets resolve EF Core, EF Core SQLite and Microsoft.Data.Sqlite.Core `10.0.9`. This report is not a provider qualification, a shipped compare-and-swap guarantee, or an E1/M1 pass.

## Question and result so far

This experiment asks whether a test-only adapter can make Elsa's existing `DefaultCommitStateHandler` commit its correctness-dependent writes through one EF Core SQLite connection and transaction, and whether a real zero-row final snapshot update then rolls earlier writes back. It also records the provider's native ambient-transaction behavior.

### Current measurement at `825e15d` (attempt 10)

The focused four-case command completed with 4 passed, 0 failed, 0 skipped (4 seconds test duration). The connection-pooling correction retained the same expected outcomes: shared transaction commit, shared transaction rollback on the real zero-row conflict, partial writes with the adapter disabled, and an unsupported ambient transaction that fails before writes.

| Case | Fresh state before | Staged immediately before conditional update | Conditional update | Independent fresh state after |
| --- | --- | --- | --- | --- |
| Shared-transaction success | Data `6636F5240B6C99FEC5FE0AC72BE5B972DAF51102CE903C21E5F53A828B000DF2`; bookmarks 1, execution logs 6, activity records 4 | Same preimage hash; bookmarks 1 (staged bookmark identity differs, asserted), execution logs 12, activity records 6; DB transaction present, ambient absent | Expected preimage is the before-state hash; attempted Data `AC40D5825526008491A5A1DEA7BA98CF37C57B68A5A276869265093F7A6D36D6`; SQLite returned a row; callback `ReaderExecutingAsync` | Data `AC40D5825526008491A5A1DEA7BA98CF37C57B68A5A276869265093F7A6D36D6`; bookmarks 1, execution logs 12, activity records 6 |
| Shared-transaction conflict rollback | Newer B Data `ACAE1C528A1FF78EE5CD741D43979A564CA17F9848F11B0E1C822A3DD8686523`; bookmarks 1, execution logs 6, activity records 4 | B's data hash unchanged; staged bookmark identity differs, execution logs 12, activity records 6; DB transaction present, ambient absent | Expected stale A `DC5E5CFC2C6E225116C8250DA22D5F1FE63F3FD783DE574538B9789E92C11D50`; attempted Data `AF41FD00FB3AD422A46B1DD5C6C8A0AD7B26A81891722F068EE9DCA23BAC8C1D`; SQLite returned zero rows; callback `ReaderExecutingAsync` | B's data hash unchanged; old bookmark identity and counts restored (1, 6, 4) |
| Transaction-disabled conflict | Newer B Data `7A5FAEF612232F27361C7BC0B2C9D6BD3ACC47B09BF584C562FF24DE41CC982B`; bookmarks 1, execution logs 6, activity records 4 | B's data hash unchanged; staged bookmark identity differs, execution logs 12, activity records 6; no DB transaction, ambient absent | Expected stale A `091EF42458AED9B46B93494EE6F2DC4807BE5C6846D99F42BECF48CA0BC0F847`; attempted Data `0600BAB9A72D485196B8A71A8D1A5CD7F7BC1FDC09B1A30B37315B305E078DA1`; SQLite returned zero rows; callback `ReaderExecutingAsync` | B's data hash unchanged; replacement bookmark and partial log writes remain (1, 12, 6) |
| Native ambient transaction | Workflow instances, bookmarks, execution logs, activity records: 0 | Initial commit stopped by EF Core `AmbientTransactionWarning`; no staged command snapshot | No final conditional update reached | Independent counts remain 0, 0, 0, 0 |

The passing TRX is `/tmp/elsa-8936-825e-trx/sqlite-commit-proof-825e.trx` (SHA-256 `1818a8f2d8e23cb738f92dc428f816519bbc048f0b38a3da8f9fddac7cf681a3`); the console log is `/tmp/elsa-8936-sqlite-proof-test-825e.log` (SHA-256 `8de9bc9f2fc0af82a5ccd387c4993eac86a82e14380e3dece9614d857d940cfa`). Full conditional SQL and variable values remain in the raw artifacts only; no serialized state or database file is checked in.

### Historical measurement at `e704c6f` (attempt 9)

At source head `e704c6fddbce8f76fab55af4e3d70b5cdc082268`, the focused four-case command completed with 4 passed, 0 failed, 0 skipped (7 seconds test duration). Build succeeded with existing warnings in untouched Identity and Admission source. The success and real conditional-conflict cases observed shared-transaction participation and rollback; the transaction-disabled case demonstrated partial writes; the ambient case stopped at EF Core's unsupported-provider warning. The passing TRX retained these sanitized measurements:

| Case | Fresh state before | Staged immediately before conditional update | Conditional update | Independent fresh state after |
| --- | --- | --- | --- | --- |
| Shared-transaction success | Data `C20FFBD4…FD5C2F53`; bookmarks 1, execution logs 6, activity records 4 | Same preimage hash; bookmarks 1 (staged bookmark identity differs, asserted), execution logs 12, activity records 6; DB transaction present, ambient absent | Expected preimage is the before-state hash; attempted Data `0A30D63E…189F0C1D`; SQLite returned a row; callback `ReaderExecutingAsync` | Data `0A30D63E…189F0C1D`; bookmarks 1, execution logs 12, activity records 6 |
| Shared-transaction conflict rollback | Newer B Data `5D8E2182…3F4FE298`; bookmarks 1, execution logs 6, activity records 4 | B's data hash unchanged; staged bookmark identity differs, execution logs 12, activity records 6; DB transaction present, ambient absent | Expected stale A `8CB56F46…91B67D58`; attempted Data `A4437720…03E1011F`; SQLite returned zero rows; callback `ReaderExecutingAsync` | B's data hash unchanged; old bookmark identity and counts restored (1, 6, 4) |
| Transaction-disabled conflict | Newer B Data `8814DF01…8493B84A`; bookmarks 1, execution logs 6, activity records 4 | B's data hash unchanged; staged bookmark identity differs, execution logs 12, activity records 6; no DB transaction, ambient absent | Expected stale A `7DA92BCD…D875EDF9`; attempted Data `F7FEDBD4…D316DBFD`; SQLite returned zero rows; callback `ReaderExecutingAsync` | B's data hash unchanged; replacement bookmark and partial log writes remain (1, 12, 6) |
| Native ambient transaction | Workflow instances, bookmarks, execution logs, activity records: 0 | Initial commit stopped by EF Core `AmbientTransactionWarning`; no staged command snapshot | No final conditional update reached | Independent counts remain 0, 0, 0, 0 |

The code also independently reloads and asserts the built-in inline variable representation in each applicable case; variable values are intentionally omitted from retained output. Data hashes above are shortened for readability; full SQL and variable values are not copied into this report. The retained artifact is `/tmp/elsa-8936-e704-trx/sqlite-commit-proof-e704.trx` (SHA-256 `90f462ba4f7c76e69f2fd4e9a6d1c7cb5e28b67bf1ddc3b17da2d895814d4b91`); the console log is `/tmp/elsa-8936-sqlite-proof-test-e704.log` (SHA-256 `52f5a1f87507f7b20548d0aa0988c120fb8a9f2c704355c8ab3677fe12ed85a9`). These raw artifacts remain outside the repository because they contain full parameterized SQL, diagnostics/stack traces and local environment paths. Inspection of the matching-hash artifacts found no serialized workflow Data or variable values.

The fixture sets `Pooling=False` so provider-pooled handles do not remain after the fixture disposes its connections, addressing temporary-file deletion failures on Windows. Attempt 10 ran on the current macOS host; no Windows test is claimed. This lifecycle correction postdates and does not replace the historical `e704c6f` measurements. The nine prior logs and both earlier TRX artifacts remain preserved.

## Scope and method

The fixture in `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/SqliteCommitTransactionParticipation/Tests.cs` uses one synthetic enabled tenant and a serial local workflow. Its test-only context factory enlists Management and Runtime EF contexts in an externally owned SQLite connection and transaction. The normal `DefaultCommitStateHandler` pipeline performs bookmark persistence, activity and workflow execution-log writes, variable persistence, and workflow-instance persistence.

The source paths behind that inventory are `src/modules/Elsa.Workflows.Runtime/Services/DefaultCommitStateHandler.cs` (write ordering and post-commit notification flush), `src/modules/Elsa.Workflows.Runtime/Services/BookmarkPersister.cs`, `src/modules/Elsa.Workflows.Runtime/Services/StoreActivityExecutionLogSink.cs`, `src/modules/Elsa.Workflows.Runtime/Services/StoreWorkflowExecutionLogSink.cs`, `src/modules/Elsa.Workflows.Core/Services/VariablePersistenceManager.cs`, `src/modules/Elsa.Workflows.Core/VariableStorageDrivers/WorkflowInstanceStorageDriver.cs`, and `src/modules/Elsa.Persistence.EFCore/Modules/Management/WorkflowInstanceStore.cs` (serialized `Data` field). The native commit transaction implementation at `src/modules/Elsa.Persistence.EFCore/Modules/Runtime/EFCoreWorkflowCommitTransaction.cs` uses `TransactionScope`; this experiment does not change it.

For the stale-run case, the fixture first writes a valid newer snapshot B through `IWorkflowInstanceManager` and a separate native EF context. It retains the original snapshot A in the execution fixture, then stages the stale run's bookmark and log writes. A test-only `DbCommandInterceptor` adds an expected-`Data` predicate to the actual final `WorkflowInstances` update, preserving the provider command and its parameters. SQLite executes that update and returns zero rows; the resulting concurrency exception propagates through the shared transaction. Fresh reads use independent contexts/connections. The interceptor is an experiment-only way to provoke and observe a conditional conflict; it does not establish that the shipped store has this predicate.

The built-in workflow-instance variable is stored in the `Variables` dictionary nested in the owning activity execution context's serialized properties. The fixture checks that persisted representation and reloads it independently. This covers the built-in storage driver's inline representation for the synthetic variable; it does not exercise `IVariableManager` definition materialization or external variable-storage drivers.

The passing-test output retains snapshot SHA-256 hashes, row counts, transaction-presence flags, callback method names, expected/attempted preimage hashes, and the conditional update's returned-row result. It does not print serialized snapshot or variable values.

## Cases and boundaries

- Shared-transaction success: the conditional update accepts A; bookmark replacement, workflow/activity logs, inline variable state, and snapshot change are read back after commit.
- Shared-transaction conflict: B is committed by the native manager before the stale run. The final predicate expects A, returns zero rows, and the transaction rolls back staged bookmark/log work. Independent reads must still show B and the old bookmarks/log counts.
- Transaction disabled: the same native calls and test interceptor run without the shared-transaction adapter. The final update returns zero rows while earlier bookmark/log commands remain committed. This is a negative control demonstrating why transaction participation matters; it is not a production configuration recommendation.
- Native ambient transaction: the existing SQLite provider does not support EF Core ambient transactions. The default warning behavior throws during the initial native commit. The test does not suppress that warning and verifies independent database counts at the failure boundary.

The native Sequence uses child-completion callbacks (`src/modules/Elsa.Workflows.Core/Activities/Sequence.cs`), and WorkflowStateExtractor serializes callback entries inside Data (`src/modules/Elsa.Workflows.Core/Services/WorkflowStateExtractor.cs`). This fixture therefore uses ordinary Sequence callbacks; it does not independently assert their persisted identities, validate missing owner/child references, or exercise callback-specific restart cuts or additional stores. The commit handler flushes post-commit notifications after the transaction; notification aftermath is outside this proof. There is no process-kill, unknown-acknowledgment, simultaneous two-process race, Studio, E2, or E3 evidence. B is a controlled manager write before stale resume. The fixture's tenant filter and persisted tenant readback prove only that this synthetic fixture was tenant-configured; they do not prove cross-tenant authorization or a tenant-bound compare-and-swap predicate. The SQL assertion that looks for `TenantId` is not itself proof that the term occurs in the `WHERE` predicate.

The experiment is bounded to SQLite and its local provider semantics. It does not cover other providers, external storage drivers, production CAS, owner/epoch fencing, migrations, or public transaction APIs. The smallest next design seam is the native connection/transaction participation required by issue #7973, not a new general transaction API. The reviewed guarded-snapshot contract is available at [the merged contract snapshot](https://github.com/elsa-workflows/elsa-core/blob/3d78ba3f20cf26039776cb91eb23a963caeeb723/core/docs/specs/native-compensation/guarded-snapshot.md); this branch predates that documentation merge and intentionally does not duplicate it.

## Verification history

All attempts are preserved under `/tmp/elsa-8936-sqlite-proof-test-*.log`. Earlier compile and fixture failures are retained rather than replaced by the passing run:

| Candidate | Log | Outcome |
| --- | --- | --- |
| `3d199ef` | `/tmp/elsa-8936-sqlite-proof-test-3d199ef.log` | Compile failed: SQLite transaction type mismatch, invalid SQLite options receiver, workflow/graph type mismatch, and missing `RunWorkflowOptions` namespace. |
| `a5b2fb6` | `/tmp/elsa-8936-sqlite-proof-test-a5b2fb6.log` | Compile failed: `RunWorkflowOptions` namespace still incorrect. |
| `5ada913` | `/tmp/elsa-8936-sqlite-proof-test-5ada913.log` | 0 passed, 3 failed: test schema was created after registry population queried `WorkflowDefinitions`. |
| `22730fe` | `/tmp/elsa-8936-sqlite-proof-test-22730fe.log` | 0 passed, 3 failed: tenant accessor replacement was overridden, leaving persisted tenant IDs empty. |
| `00075f6` | `/tmp/elsa-8936-sqlite-proof-test-00075f6.log` | 0 passed, 3 failed: repeated interceptor callback injection duplicated parameters; variable setup was incomplete, and later source review found the fixture also targeted the workflow root bag rather than the actual nested activity-context owner. |
| `d355477` | `/tmp/elsa-8936-sqlite-proof-test-d355477.log` | 2 passed, 1 failed; the two passing negatives were provisional because the fixture wrote/read a root property bag instead of the actual nested activity-context variable. |
| `bf0ac63` | `/tmp/elsa-8936-sqlite-proof-test-bf0ac63.log` | 3 passed, 0 failed after correcting the nested variable representation; no ambient test or retained numeric output yet. |
| `c02308c` | `/tmp/elsa-8936-sqlite-proof-test-c02308c.log` | 4 passed, 0 failed in the focused filter; passing-test numeric output was not retained by the console logger. |
| `e704c6f` | `/tmp/elsa-8936-sqlite-proof-test-e704.log`; TRX `/tmp/elsa-8936-e704-trx/sqlite-commit-proof-e704.trx` | 4 passed, 0 failed, 0 skipped; sanitized numeric measurements above retained by TRX. |
| `825e15d` | `/tmp/elsa-8936-sqlite-proof-test-825e.log`; TRX `/tmp/elsa-8936-825e-trx/sqlite-commit-proof-825e.trx` | 4 passed, 0 failed, 0 skipped; pooling-disabled fixture and current measurements above. |

The prior successful run used:

```text
/Users/sipke/.local/share/dotnet-build-slots/bin/dotnet test core/test/integration/Elsa.Workflows.IntegrationTests/Elsa.Workflows.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~SqliteCommitTransactionParticipation -m:1 -nologo
```

The attempt 9 command was:

```text
/Users/sipke/.local/share/dotnet-build-slots/bin/dotnet test core/test/integration/Elsa.Workflows.IntegrationTests/Elsa.Workflows.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~SqliteCommitTransactionParticipation -m:1 -nologo --logger 'trx;LogFileName=sqlite-commit-proof-e704.trx' --results-directory /tmp/elsa-8936-e704-trx
```

The attempt 10 command at source head `825e15dcb41977aa8d7025564f746a3298f4f134` was:

```text
/Users/sipke/.local/share/dotnet-build-slots/bin/dotnet test core/test/integration/Elsa.Workflows.IntegrationTests/Elsa.Workflows.IntegrationTests.csproj --no-restore --filter FullyQualifiedName~SqliteCommitTransactionParticipation -m:1 -nologo --logger 'trx;LogFileName=sqlite-commit-proof-825e.trx' --results-directory /tmp/elsa-8936-825e-trx
```

Attempt 9 added sanitized passing-output capture and synchronous context disposal if transaction enlistment throws. Attempt 10 changes only the fixture's SQLite pooling setting. No broad integration suite, full compensation matrix, or Studio journey is claimed.

## Disposition and next readiness gates

The bounded feasibility question is answered: an experimental shared local connection/transaction can roll back the exercised native stores with a real rejected final update. The shipped SQLite ambient implementation remains unsupported. Integrate the smallest participation seam with #7973; do not infer a public API decision or production support from the test adapter. The program lead owns readiness and integration; the following owners must close their gates before M1 activation:

| Follow-up owner | Required evidence and exit condition |
| --- | --- |
| Runtime/Persistence: guarded snapshot | Accept connection/transaction ownership, disposal and notification-failure behavior for the selected profile; census every correctness-dependent store/driver and prove participation. Implement/review row-owned marker/revision/owner epoch and a tenant-bound conditional write, raw fail-closed admission, pending/committed receipt identity, migration and backout. Prove stale-owner rejection, original-byte preservation, uncertain commit reconciliation and a no-final-rewrite abort path. The test-only Data predicate is not that implementation. Preserve unmarked workflows and third-party store compatibility. |
| Runtime/Persistence: fault handoff | Keep native RecoverFromFault bookkeeping under middleware ownership. Prove staged recovery work and the first pre-effect checkpoint occur after repaired counters, with restart cuts, intervention-only persistence, partial-handler failure and commit/ack failure negatives. A callback queue or exception wrapper alone is insufficient; demonstrate one pinned outward handoff and no final save of partially mutated memory. Use the existing-seam candidate first; justify any new hook from failed native evidence. |
| Security/Core/Studio/Hosting: E2 | Design task #8948 may proceed independently now. Integrated proof requires the guarded seam: persistent tenant/purpose-bound protection keys across a new process, safe summary/detail and source-suppression boundaries, separately authorized detail and validated principal-to-tenant binding. Deny unsupported profiles before effects; prove tamper/missing-key, generic disclosure/import, legacy-view and cross-tenant negatives, and preserve opaque bytes/obligations. Do not activate from encryption-only evidence. |
| Core/Studio: E3 and integrated M1 | Exercise actual registered activity identities/ports, captured-data mapping, publication validation, repeated-occurrence identity and strict pinned definition/handler resolution. Qualify the real Studio author/publish/run/inspect route and safe tenant/environment cache changes with the accepted E1/E2 profile. Require Studio-to-backend-to-persistent-store process-kill/new-process evidence and accessible occurrence-to-attempt visualization before M1 acceptance. This transaction fixture is not that journey. |

The recorded commands use this machine's build-slot wrapper. On another host use its approved dotnet entrypoint with the same filter and TRX logger; do not bypass shared-machine scheduling. Retaining passing-test output on the first proof run avoids a repeat solely to recover observations. Exact commands and all failed attempts above remain provenance. E1, M1 and later milestones stay open until their own full criteria pass.
