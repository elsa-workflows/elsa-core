# Guarded native workflow-instance snapshots

Status: Proposed persistence contract for #8933, 21 October 2026. This is a design recommendation, not an implemented API, schema, provider capability, or readiness result. The physical architecture ADR remains Proposed. The source baseline is commit 2513472e49437aa9688a0436240348858e83fdbf; the E1 report remains an evidence-only draft in [#8932](https://github.com/elsa-workflows/elsa-core/pull/8932).

## Problem and boundary

E1 reproduced two native persistence failures that a journal-only revision cannot address. A stale workflow-instance object can overwrite a newer owner/revision because the write has no conditional fence. Separately, the EF loader catches outer decompression/deserialization errors and returns an instance with default state; ordinary save then replaces the original bytes with serialized null. Unknown inner journal schema and missing required work/callback references also reach state application/extraction before a durable blocked outcome is established. See the [E1 report](https://github.com/elsa-workflows/elsa-core/pull/8932) and [source evidence](evidence.md).

The logical journal remains in the proposed root workflow-state snapshot. It is not the place to store the only marker, row revision, owner epoch, or evidence that a commit was acknowledged: all of those must remain readable when serialized Data is corrupt or unknown. This contract covers the native owning instance row and the route from guarded load through native commit. It does not design a second scheduler, external-effect ledger, generic database unit-of-work, or Studio API.

The following are measured current behavior, not future guarantees:

| Native seam | Current behavior and design consequence |
|---|---|
| [WorkflowInstance](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Management/Entities/WorkflowInstance.cs#L14-L35) | Version is the workflow-definition version; WorkflowState is the serialized runtime state. Neither is a durable write revision. |
| [EF instance-store load](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore/Modules/Management/WorkflowInstanceStore.cs#L51-L54) and [hydration](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore/Modules/Management/WorkflowInstanceStore.cs#L245-L280) | Find applies OnLoad after reading; OnLoad catches failures and assigns default state. Save serializes that state back into Data. A guarded marker must be checked before this fallback. |
| [Public store contract](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs#L132-L172) | Save, Update, SaveMany and Delete have no revision/owner receipt. Their signatures are used by custom store implementations. Keep them compatible. |
| [Generic EF store](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore.Common/Store.cs#L136-L152) and [bulk/update/delete paths](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore.Common/Store.cs#L179-L224) | Save performs key-exists then full-row Modified/Added save; SaveMany uses custom bulk upsert. Update attaches and marks the row Modified; delete uses direct EF delete operations. A guarded CAS helper cannot protect these paths by itself. |
| [Runner load](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Runtime/Services/LocalWorkflowClient.cs#L86-L90) and [commit handler](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Runtime/Services/DefaultCommitStateHandler.cs#L23-L41) | The client loads an instance for the runner. The default commit saves bookmarks, logs, variables and then the workflow state. Any rejected late instance CAS must not leave correctness-dependent earlier writes behind. |

## Recommended contract

Recommend one opt-in guarded-snapshot capability owned by the native workflow-instance persistence boundary. Keep names, CLR types, columns and exact provider API open for implementation review. Do not add required members to IWorkflowInstanceStore or claim existing stores support this capability.

The capability has three responsibilities:

1. Read persisted guard metadata and raw serialized state before ordinary hydration. For an unmarked legacy row, retain existing behavior. For a marked row, return either valid decoded/admitted state plus a store-issued write receipt, or a typed nonterminal blocked result. The store retains original bytes and codec marker; no fallback state reaches the runner.
2. Atomically claim local process ownership and conditionally commit a guarded snapshot. Every accepted write checks the exact instance, trusted owning tenant, expected storage revision and expected owner epoch in the database write itself. It returns an explicit committed receipt or typed conflict/unknown outcome.
3. Enforce the guard for every native path that can alter or delete a marked row. Legacy methods may continue unchanged for unmarked rows. A write to a marked row without a valid current receipt or explicitly defined guarded lifecycle transition fails closed.

The trusted receipt is created by persistence, travels in runtime-owned execution context from load through state extraction/manager remapping and commit, and is never accepted from workflow input, an exported state file, a request tenant field or root Properties. It is not an authorization grant. Persistence rechecks current tenant, row marker, revision and owner epoch. An optional capability beside the existing store contract is the compatibility direction: third-party providers need not implement it for existing workflows, but protected publication/activation must be rejected before the first effect when the selected store cannot provide the guarded contract. Whether the capability is public or internal remains an API review item; public names require a concrete supported caller scenario.

### Storage-owned identity

The guard is proposed as additive metadata on the same workflow-instance row, independently readable of Data:

| Metadata | Contract |
|---|---|
| Guard format/state | Zero or absent means legacy-unmarked. A known nonzero format identifies a row whose snapshot uses guarded hydration and writes. An unknown nonzero format blocks. Setting the marker and first armed journal/work transition is one durable commit before any effect. Do not infer or clear this marker from a decoded property bag. |
| Storage revision | Monotonic row-write version for guarded state/ownership mutations. Increment with each accepted guarded mutation. It is distinct from logical JournalRevision, which advances only with journal transitions and remains inside the journal. Neither is WorkflowInstance.Version or UpdatedAt. |
| Owner epoch and identity | OwnerEpoch advances atomically on an allowed owner handoff. OwnerIdentity denotes one runtime process incarnation; a receipt binds to that identity and epoch. Storage revision detects competing row changes; the epoch fences a prior owner after a new owner is admitted. |
| Pending/committed transition receipt | Persistence issues a transition ID and binds it to the canonical transition hash and expected revision before the uncertain write is attempted. The trusted runtime must receive that ID/hash first. The prepared intent is durable and discoverable after restart; if its acknowledgement is uncertain, no transition write or effect proceeds until that exact intent is reconciled. Retain the pending ID/hash and its original expected revision across restart. Once committed, retain the same ID/hash plus its original committed storage revision until acknowledged/reconciled. A stable OperationKey identifies a participant operation; it does not prove that this database commit is the intended one. |

Existing rows migrate as unmarked without parsing Data. A provider missing guard metadata, an unknown marker, or an incompatible schema cannot be treated as legacy for a protected activation. Additive migration, provider defaults and rollback behavior require a reviewed implementation plan. Keep metadata out of workflow input/output and general-purpose serialization.

### Guarded load and admission

The load decision precedes OnLoad fallback and WorkflowState extraction:

| Result | Required behavior |
|---|---|
| Not found or tenant-invisible | Preserve existing not-found/visibility behavior; do not disclose another tenant's marker or status. |
| Legacy unmarked | Use the existing load path and APIs. Do not claim it is fenced or compensation capable. |
| Guarded and valid | Decode outer snapshot; decode supported journal format; validate exact definition/activity pins and every required queued-work, active-context, callback-owner and callback-child reference. Only after complete admission return state plus trusted receipt. |
| Guarded but invalid | Return a typed nonterminal blocked inspection result with a safe reason code and revision. Preserve original opaque bytes, compression marker, journal evidence and obligations unchanged. Do not return raw bytes/captures through API, log or general projection. Do not fall back to null/default state, partially extract, resume, save an empty replacement or invoke participant code. |
| Guarded metadata unreadable/unknown | Block before execution. A system unable to establish whether a row is guarded must not assume it is legacy. |

Outer JSON/decompression/codec failure, valid outer data with unknown inner journal schema, and missing pinned/work/callback references are separate blocked reasons. This prevents a valid outer object from masking a missing obligation and prevents a decode failure from erasing the only marker. Public projection is safe status only; protected snapshot contents remain internal.

### Claim, guarded write and result semantics

The store executes a conditional write equivalent in meaning to:

  instance ID = expected ID
  AND tenant = trusted owning tenant
  AND guard format = expected supported format
  AND storage revision = expected revision
  AND owner epoch = expected epoch
  AND owner identity = receipt owner

The trusted runtime first obtains a persistence-issued transition ID/hash in a durably prepared intent. It then attempts the guarded write with that exact intent. The accepted transaction writes new Data/codec, required native work/callback references, journal transition, next storage revision and canonical committed receipt, retaining the intent identity. Zero affected rows is a conflict, never success. Do not refresh the caller token and retry a stale whole snapshot.

Keep these internal outcomes distinct; exact type names remain open:

| Outcome | Meaning and next action |
|---|---|
| Committed | Exact transition, revision and canonical receipt are durably acknowledged. Only this permits a dependent forward/undo effect. |
| Revision conflict | Another guarded write won. Do not run an effect; reload/admit canonical state under valid ownership or remain blocked. |
| Owner conflict | Receipt belongs to a previous owner epoch/identity. Reject the write; do not infer that the old process is dead from a failed CAS. |
| Blocked | Snapshot/schema/reference/tenant or store capability cannot be safely admitted. Preserve state and expose only safe inspection. |
| Unknown preparation/write acknowledgement | The storage call may have prepared or committed but the caller cannot tell. Do not run the effect or invent an ID. Reconcile the already-known persistence-issued ID/hash against canonical pending/committed metadata under valid current ownership. If the ID was never received and cannot be discovered by the canonical transition hash, remain blocked. Exact committed ID/hash proves this transition committed at its recorded original committed revision. A pending intent may retry only with that same ID/hash after explicit reconciliation binds it to the current row revision/owner; a proven absent intent may be prepared again only when the storage contract proves no attempt/effect occurred. Later or conflicting state without matching proof remains blocked. |

The pre-effect boundary waits for the exact acknowledged or reconciled commit receipt. It must not invoke activity code after a revision conflict, blocked load or unresolved preparation/commit acknowledgement. On owner claim, increment current StorageRevision/OwnerEpoch as required but preserve the pending transition ID/hash and its original committed revision; an ownership receipt must not overwrite the transition receipt. Current row revision and the revision at which the transition committed are distinct. Participant OperationKey/UndoKey and database transition ID are separate identities. External participant completion can remain uncertain after a committed local intent; idempotency, closed-key behavior and reconciliation remain required, and local CAS does not fence a remote effect.

### Owner handoff

The first support candidate is one local runtime host and serial instance execution. A new process may advance OwnerEpoch only after the host/supervisor supplies positive evidence that the previous owning process stopped and no second local owner remains. A lock check or failed heartbeat alone is not process-death proof. If ownership cannot be established, inspection remains blocked and no effect is invoked.

Do not invent a timeout lease or claim distributed fencing. A paused old host may still resume and issue a remote request; a database epoch alone cannot revoke that request. Distributed/multi-host ownership requires a later policy and proof, including participant-side fencing or a contract that makes late requests safe. In the bounded local profile, kill/restart demonstrates process death, but the independent participant still must deduplicate stable operation keys and define late/closed-key behavior before broader support.

## Tenant binding

Use authenticated server tenant context and row owner loaded from trusted storage. Never use request-supplied TenantId as mutation authority. The current EF query filter exposes rows for the exact context tenant, tenant-agnostic "*", and legacy null rows for the empty/default tenant; [TenantsOptions.IsEnabled](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Tenants/Options/TenantsOptions.cs#L9-L19) defaults false, and [ElsaDbContextBase](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore.Common/ElsaDbContextBase.cs#L47-L60) disables filtering when the option is absent. Read visibility is not ownership.

For the first guarded support profile, require `TenantsOptions.IsEnabled == true` and a trusted server tenant context; an explicitly configured default-tenant host may use its authoritative empty tenant. The original E1 fixture with filtering disabled is not a qualifying guarded profile. Unmarked workflows retain current behavior. Support for guarded requests with disabled/unavailable tenant filtering is deferred for E2 security review; do not weaken the exact normalized owner predicate in the actual write. Treat null and empty as equivalent only for existing default-tenant compatibility. An asterisk-agnostic row is readable across tenants but has no unique compensation owner; block guarded mutation until a separate explicit owner policy exists. Never use request-supplied TenantId as mutation authority. Trusted host maintenance needs separate explicit authority and audit. CAS, owner claim, status transitions, import and deletion all apply the tenant predicate in the actual write, including bulk/custom SQL paths that bypass EF query filters.

## Native mutation paths that must be guarded

This is the current Core API inventory at the pinned source baseline, not a claim about host-written SQL outside Elsa APIs.

| Path | Source seam | Rule for a marked row |
|---|---|---|
| Find/load and ordinary Save/Add/Update/SaveMany | [EF WorkflowInstanceStore](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore/Modules/Management/WorkflowInstanceStore.cs#L51-L54), [IWorkflowInstanceStore](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs#L140-L172) | Hydrate before fallback; legacy writes without a current receipt are rejected. Guarded save conditionally commits the complete row snapshot. SaveMany must not replace guarded rows through bulk upsert. |
| Generic Save, Update, partial update, delete and bulk delete | [Generic Store](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore.Common/Store.cs#L136-L152), [update/delete](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore.Common/Store.cs#L466-L493), [bulk delete](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore.Common/Store.cs#L664-L692) | Every replace path participates in the guard. Delete is refused while eligible/unresolved obligations or protected history remain; final purge needs accepted retention policy and audited no-obligation check. |
| Timestamp and interrupted status | [EF store](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore/Modules/Management/WorkflowInstanceStore.cs#L158-L216) | Partial timestamp changes and TryMarkInterrupted are guarded lifecycle writes, not routes around row revision/owner epoch. Restart uses an explicit claim/interrupt transition. |
| Manager mutation and deletion | [WorkflowInstanceManager](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Management/Services/WorkflowInstanceManager.cs#L47-L127) | Save/Update carry the runtime receipt; Delete/BulkDelete reject protected unresolved state and use conditional audited cleanup only after policy permits purge. |
| Run, business cancel, import, export and delete | [LocalWorkflowClient run](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Runtime/Services/LocalWorkflowClient.cs#L86-L90), [cancel/import/delete](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Runtime/Services/LocalWorkflowClient.cs#L153-L204) | Resume requires guarded admission. Ordinary cancel cannot substitute for accepted O2 cancellation. Import cannot forge, clear or replace a marked row receipt/obligation. Delete is refused until guarded cleanup is allowed. Export never grants mutation authority. |
| API import and bulk delete | [Import](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Api/Endpoints/WorkflowInstances/Import/Endpoint.cs#L108-L168), [bulk delete](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Api/Endpoints/WorkflowInstances/BulkDelete/Endpoint.cs#L31-L64) | Import/replace refuses protected existing rows unless a future guarded restore contract validates them. Bulk delete checks each marked row; Finished status alone is insufficient. |
| Alteration/retry and interruption recovery | [Alteration retry](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Alterations/Endpoints/Workflows/Retry/Endpoint.cs#L43-L77), [interrupted marker](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Persistence.EFCore/Modules/Management/WorkflowInstanceStore.cs#L201-L216) | Generic retry/scheduling cannot inject work into a marked instance outside its guard. Interrupted recovery first proves owner transition and complete restore admission; unsupported operation blocks. |
| Workflow dispatch and outbox marker cleanup | [Processor](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Runtime/Services/WorkflowDispatchOutboxProcessor.cs#L175-L205), [delivered-item cleanup](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Runtime/Services/WorkflowDispatchOutboxProcessor.cs#L401-L432), [marker cleaner](https://github.com/elsa-workflows/elsa-core/blob/2513472e49437aa9688a0436240348858e83fdbf/core/src/modules/Elsa.Workflows.Runtime/Services/WorkflowDispatchOutboxMarkerCleaner.cs#L20-L36) | Current code sends before cleanup, deletes the outbox row before marker removal, then catches/logs marker-cleanup failure. A marked row requires an admitted receipt-checked handoff/cleanup protocol with durable retry/dedup before Send; never fall back to Save or silently drop the marker. Until then, reject guarded dispatch before Send and exclude guarded rows with pending dispatch markers from M1. The outbox is not a compensation participant/recovery ledger. |

This inventory is the minimum Core API audit for a future implementation, not proof each path currently checks a guard. Tests must include these paths. Arbitrary direct database edits by a host are outside the Core API contract; after such edits, guarded admission must still detect invalid metadata/references and block rather than repair by omission.

## Public and persisted-data compatibility

Keep current IWorkflowInstanceStore signatures and behavior for unmarked rows. Do not use WorkflowInstance.Version as a fence or expose the storage receipt as workflow input, ordinary API state, Studio DTO or log field. Do not add a breaking public interface member in Elsa 3. If implementation requires a public optional capability, document its caller scenario, keep the old interface working, and ensure absence disables only the protected feature before any effect.

New internal row metadata is an additive schema change and must have non-destructive defaults for old rows. Never deserialize an old row to decide whether it is guarded. Unknown guarded schema, missing metadata or unreadable guarded payload blocks. Raw bytes and protected captures are internal recovery evidence, not generic output. Retention and purge cannot clear the marker while eligibility, uncertainty, audit or pinned recovery references remain.

## Atomic boundary and #7973

The instance-row CAS must not run after required bookmarks or variable state have already committed separately. DefaultCommitStateHandler writes bookmark diffs, logs, variables and then instance snapshot inside IWorkflowCommitTransaction. The configured EF wrapper uses TransactionScope; E1 observed that Microsoft.Data.Sqlite rejects ambient transactions. The default runtime transaction is also no-op. A successful single-row CAS is necessary but not sufficient for an E1 pre-effect commit.

Before an M1 production leaf is Ready, the next bounded E1 proof must identify the exact persistence records required to resume the protected serial workflow, then prove those records and the guarded instance transition commit/roll back as one provider-supported unit. For candidate SQLite, EF Core's documented cross-context transaction requires contexts to share both a DbConnection and DbTransaction; Microsoft.Data.Sqlite recommends ADO.NET transactions because it does not support System.Transactions. Link and reuse [#7973](https://github.com/elsa-workflows/elsa-core/issues/7973), the existing general request for an application-supplied connection/transaction seam; this design does not duplicate that platform enhancement.

If the bounded proof cannot enlist every correctness-dependent write, stop before M1 readiness and propose a specific durable repair protocol or revised provider/architecture. Never let a failed/late CAS leave a bookmark, variable, dispatch or work record that makes canonical workflow state inconsistent. Logs may be excluded only if proven irrelevant to recovery. Do not describe two separate saves as atomic.

## Milestones and proof plan

E1 report #8932 is Partial; M1 #8891 remains Not Ready; O1–O3 remain accepted product policy; E1–E3 remain unproved; the root-instance-journal ADR remains Proposed. This contract does not change those statuses or authorize code, schema, migrations, production APIs, experiments or provider claims.

| Stage | Guarded-snapshot scope |
|---|---|
| M1 / E1 before Ready | One local runtime host at a time; one serial scope and explicit participant; guarded load, owner claim, storage revision/epoch CAS, pre-effect acknowledged/reconciled commit receipt, fail-closed outer/inner admission, native mutation guards, trusted tenant negatives with `TenantsOptions.IsEnabled == true`, raw-byte preservation, unmarked legacy compatibility, process-kill restart and bounded required-store transaction proof for one named candidate profile. Guarded dispatch remains rejected before Send until durable receipt-checked handoff/cleanup and retry/dedup are qualified. No arbitrary provider or distributed-owner claim. |
| M2 | Remote effect unknown-outcome lookup/reconciliation, stable participant keys, closed-key late results and bounded recovery retries. Local commit receipt does not solve participant uncertainty. |
| M3 | Loops/nesting, wider owner/descendant selection, cancellation composition and nonserial scheduling. |
| M4 | Operator retry/reconcile/repair commands, permission design, audit UI and intervention controls. |
| M5 | Provider matrix, schema upgrades/upcasters, general retention/purge and release support. |

Required M1/E1 acceptance cases:

1. Two independent EF contexts read the same storage revision and owner epoch. The first guarded writer wins; stale revision and stale owner are both rejected by the actual row write, with no dependent participant invocation or partial correctness-dependent side-store write.
2. Kill the owning backend before/after durable intent preparation, at pre-commit, committed-but-unacknowledged and post-commit/pre-effect cuts. If the prepared ID was not acknowledged, recover it by canonical transition hash or remain blocked. A new local process advances epoch only after positive proof the old process stopped. It preserves pending ID/hash and original committed revision while advancing current row revision. The exact canonical receipt/hash permits the dependent effect once; a pending intent retries only with the same ID/hash after reconciliation binds it to current row revision/owner; mismatching/later state or unresolved owner remains blocked. A participant OperationKey alone is not commit proof.
3. Cross-tenant controls attempt load, claim, CAS, interrupt, import and delete by ID under the required first-profile `TenantsOptions.IsEnabled == true` configuration. Tenant B cannot mutate tenant A. Default-tenant null/empty compatibility is explicit and server configured. Asterisk-agnostic rows may be read according to existing policy but are blocked for guarded mutation. The original E1 fixture with filtering disabled is not qualification; disabled-filter guarded requests stay deferred for E2 review and cannot relax request authority.
4. Corrupt outer JSON, valid JSON with wrong/unknown compression, unknown inner schema, missing pinned definition/activity, missing required work, missing callback owner/child and unknown guard format each produce typed nonterminal blocked inspection before extractor omission, runner or participant invocation. Raw snapshot bytes, codec and obligation evidence/hash remain unchanged after attempted inspect/resume/save; no null/empty replacement occurs.
5. Exercise normal Save, Update, SaveMany, partial timestamp, TryMarkInterrupted, delete, bulk delete, runtime cancel, instance import, alteration retry, interruption recovery and outbox marker cleanup against a marked unresolved row. Each follows a defined receipt-checked transition or refuses; none clears guard or drops required work. Verify guarded dispatch is rejected before Send unless a full receipt-checked handoff/cleanup protocol is supported. For supported cleanup, verify failure remains durably pending/retryable with the same item identity and dedup; it must not be reported as completed cleanup or silently redelivered. A test provider implementing only today's interface still runs ordinary unmarked workflows and rejects protected activation before effect.
6. A deliberately disabled guard control demonstrates stale overwrite and corrupt-state null-resave risk. It is test-only and not a production configuration.
7. The accepted profile proves transaction rollback on CAS conflict across the full correctness-dependent native record set. If it does not, E1 records the exact partial mutation and M1 remains Not Ready.

The owner/revisit trigger for distributed or paused-host takeover is new support for multi-host/failover execution or evidence that a same-host supervisor cannot prove exclusive process termination. Revisit before expanding the host profile. A separate product decision is required if block-on-uncertain-owner is unacceptable; do not silently substitute an expiry lease.

## Mechanism references

- [EF Core concurrency tokens](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) describe conditional update behavior and application-managed tokens; this is a mechanism reference, not proof that Elsa detached write paths use it.
- [EF Core cross-context transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions) require contexts to share a DbConnection and DbTransaction; this connects to, but does not resolve, #7973.
- [Microsoft.Data.Sqlite ADO.NET limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/adonet-limitations) state that System.Transactions is unsupported; use a provider-supported ADO.NET transaction proof.
- [EF Core ExecuteUpdate/Delete](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete) notes bulk operations do not automatically use change-tracker concurrency tokens; callers must add predicates and inspect affected-row counts.
