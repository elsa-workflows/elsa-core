# Durable event admission with isolated local execution

## Status

Implementation decision for Task #8658, based on the [approved bounded admission design](https://github.com/elsa-workflows/elsa-core/issues/8229#issuecomment-6049219598). This record describes the supported contract and its implementation; it does not assert that current-head hosted verification or live pilot acceptance has passed. The acceptance receipt and review bind the exact tested source separately.

## Context

A provider event must become durably accountable before acknowledgement eligibility. An event envelope, a Pending workflow row, or a persisted StartAuthorized flag cannot establish that a caller can safely invoke execution. Workflow creation has saved notifications; a matching row does not establish their completion. PostgreSQL admission and workflow management/runtime persistence have separate commits. A delayed caller can retain authorization after subscription withdrawal.

## Decision

Use three modules: `Elsa.Workflows.Admission`, its distinct `AdmissionElsaDbContext` EF Core persistence module, and the PostgreSQL provider. Reuse existing workflow management/runtime persistence, credential infrastructure, and exact-instance outbound grants. The ledger stores no credential values and grants no outbound authority.

The initial supported topology is one isolated, synchronous in-process LocalWorkflowRuntime execution host. Concurrent ledger/controller processes and sequential execution-host restart are supported proof topologies; concurrent execution hosts, distributed/Proto.Actor invocation, arbitrary management routes and unaudited store-sharing writers are unsupported. They require a concrete later requirement and review, not an implied universal adapter obligation.

### Admission and state

Trusted configuration binds tenant, environment, stable logical installation, channel, logical subscription and a pinned published workflow definition/full-content fingerprint. Installation identity survives reconnect, token rotation and reinstall. Edits retain logical subscription identity; retirement cannot reset that identity. Intentional fan-out uses independently authorized subscriptions. A fresh subscription has an explicit trusted activation boundary; it does not imply replay of old events.

An atomic admission transaction applies human-message/loop/time filters, unique identity, configuration/activation epoch and capacity. Only a definite committed insertion or a verified duplicate is acknowledgement eligible. The immutable admitted configuration, event digest and payload digest survive later reconfiguration and payload cleanup. Payloads cannot select trusted routing or authority.

`Admitted → Creating → Materialized → StartPreparing → StartAuthorized → ExecutionObserved` records progress. Creating durably allocates the instance ID before actual insert-only workflow creation. Materialized requires definite insert and notification completion plus a recorded initial-state digest. ExecutionObserved requires real final commit, trailing state save, complete owner unwind and a definitively recorded full-state/bookmark checkpoint. It can mean Suspended; it is not necessarily terminal. Finished workflow status qualifies Completed, with business outcome still distinguished by substatus. Unknown insert, saved notification, permit or final-write/checkpoint outcome requires RecoveryRequired, never effect replay.

### Private authority and Core guard

Only definitive first permit-commit success may bind a private process-local one-shot invocation. Persisted state, readback, a restart, a duplicate, or lease expiry never reconstructs it. Core's optional guard remains neutral: public runner preparation preflights ownership before custom preparation where possible; runner authorization precedes logger/start notifications; public workflow/activity pipeline Execute/Pipeline/Setup/Build delegates guard at invocation, including cached and changed builders. Unowned hosts retain existing behavior.

The admission registry protects exact prepared context references and immutable trusted admission/revision/attempt/pin identity. Revalidation includes typed input/collection enumeration order and comparer, memory, ordered graph relationships, scheduler plan, ExecuteDelegate and completion delegate/owner/child identities. Numeric CLR kinds, JSON versus CLR and negative zero remain distinct; arbitrary getters, custom objects, unsupported collection kinds and nonfinite numbers are rejected. Definition JSON canonicalization reflects stored JSON representation and does not relax executable value fingerprints.

Admission replaces earlier custom pipeline setup factories without invoking them, owns and freezes both actual compositions before exposing them, rejects contributors, and audits resolved preparation services, providers, notifications, invoker, loggers, observer, storage drivers and cycle registry. Workflow composition is execution-cycle tracking → persistent-variable load → Core exception handling → default scheduler. Activity composition preserves logging → exception handling → execution logging → notifications → default invoker. Normal commit/bookmark/variable notification semantics remain; asynchronous outbox/heartbeat runtime execution is excluded. Freeze rejects Setup before callbacks or component construction.

Only the built-in scheduler may enter the internal built-in invoker/raw activity-pipeline lane, during the private exact-context executing phase. Both public invoker overloads and every public activity delegate deny owned execution even then. The phase is retired in finally. No public flag, ambient permission or serialized ticket creates authority. Trusted observer access to a private pre-consumption context is not a public capability API; the contract does not sandbox hostile trusted extensions or concurrent graph mutation. Copied/same-ID contexts cannot bind authority, and exact-context concurrent/sequential reuse cannot obtain a second invocation.

### Continuation and management

The actual runtime client factory wraps LocalWorkflowRuntime. Owned initial run/control requests and dispatcher requests cannot mint authority. Owned Cancel/Delete/Import/UpdateBookmark and variable-management writes fail before effects. Cancellation checks the exact context before WorkflowCancelling. Bookmark upsert checks both the existing row's actual owner and the proposed owner, with tenant-agnostic identity lookup. Generic workflow management/import/alteration routes, connection lifecycle mutations, tenant deletion and competing autonomous executors are excluded or fail closed at startup/service boundaries.

Legitimate continuation requires ExecutionObserved, exact unconsumed bookmark lineage, full persisted state plus durable bookmark fingerprint, an existing owning activity and a serialized local owner after prior final writes/unwind. Arbitrary state/bookmark imports and forged Suspended snapshots cannot manufacture lineage. Internal variable persistence uses separately audited storage; raw persistence interfaces are trusted host extensions, not a universal hostile-writer firewall.

### Withdrawal, recovery and retention

Supported withdrawal records authoritative ledger withdrawal before actual workflow retract. An external retract failure leaves the subscription withdrawn with reconciliation required. Before permit linearization withdrawal prevents issuance; an already issued delayed authority may still run once. Resolution/cleanup cannot retire its owner merely because no workflow appears active.

Recovery classifies uncertainty and exposes bounded safe health pages without replay. An exclusive ID cursor walks an existing nonterminal set; restart from the beginning to discover later insertions. Resolved requires audited operator confirmation of no unknown effects and every outstanding/delayed owner's quiescence, plus actual local-owner and persisted-execution checks. Neither caller booleans nor lease expiry establish remote effect completion.

All policy values are explicit activation inputs: retention, identity horizon, age/skew/late handling, capacity and cleanup authority. Horizon must be strictly greater than maximum age plus skew. PostgreSQL admission and terminal timestamps round upward to microseconds; terminal retention origin cannot precede persisted admission. MaximumEventAge cannot widen in an existing namespace: forgotten identities must not become eligible again. Cleanup atomically checks state/revision/terminal clock and erases payload then identity; ownership survives while its instance remains startable. Allocated-instance tombstones remain charged against retained-record capacity. Active reservations release once on qualified terminal transition; non-owned terminal records may release retained capacity once on actual deletion. Payload and event-ID byte ceilings also bound individual records.

### Bootstrap

The same guarded inactive host provisions a fixed allowlisted JSON artifact and trusted subscription. A selected-provider session advisory lock covers logical definition and subscription across insert, normal publication/indexing and ledger verification. The narrow insertion reuses the exact EF management state codec/shadow data. Only a definitive Inserted outcome may publish. ExistingMatch without verified ledger state stays inactive/reconciliation and never repeats uncertain notifications. Repeat verified bootstrap reloads and checks full content without republishing. Competing mismatched logical-definition/version rows are rejected. Arbitrary external writers are excluded throughout this chain; advisory locking is not a cross-store transaction or an arbitrary database-writer fence.

## Consequences and proof

The conservative boundary can retain RecoveryRequired records and charged ownership tombstones until explicit reconciliation; it favors no automatic replay over automatic availability. Numeric/live owner policy, real Socket Mode acknowledgement/reconnect, legacy transport compatibility, remote post uncertainty, live threaded reply and operator acceptance remain later pilot obligations. See the [supported setup and recovery guide](../../core/docs/guides/durable-event-admission.md).

Hosted proof must use real PostgreSQL, independent ledger/controller processes, one real execution host, deterministic barriers and actual kill/sequential restart. Actual workflow insertion, notifications, execution, persistence, ownership, continuation, retention and bootstrap predicates bind exact source, fixture/process/container identities and sanitized per-case receipts. Missing cases/skips/zero results are failures; passing store concurrency alone cannot establish execution or transport acceptance.
