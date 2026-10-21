# E1 result: Partial, candidate profile not qualified

Experiment for [#8930](https://github.com/elsa-workflows/elsa-core/issues/8930), against design `51d316882e6a79376145aef1283aa4004a3f400d`. The actual native execution/store experiment demonstrates useful snapshot/replay mechanisms, and reproduces blocking transaction, fencing, hydration, reference-admission and fault-bookkeeping gaps. **E1 does not pass. M1 remains Not Ready.** No production source, public API, schema, application startup or package/release target changed. The physical ADR remains Proposed.

## Configuration and proof boundary

- Source containing the final execution fixture: `f3d4b31863dc7e2a029d6e93454a25a905e9b813`. Final adversarial controls and null-safe resave diagnostic: `17a27e78d6c6afaef1b9c1a302b52442d30894f1`. Later report commits do not change runtime behavior.
- r4 ran the build-4 working tree before the first experiment commit, with the seed-before-first-work ordering. Its native C# source can be reconstructed from `f3d4b31863dc7e2a029d6e93454a25a905e9b813` using [r4-native-source.patch](r4-native-source.patch), validated against that revision in an isolated directory. The old r4 controller source was not separately frozen; its original events/assertion results remain in the raw evidence. The authoritative final reproducer is r5's frozen native fixture and controller at `f3d4b31863dc7e2a029d6e93454a25a905e9b813`; r4 is retained as failed developmental evidence, not substituted for r5.
- .NET SDK `10.0.300`, `net10.0`; EF Core SQLite and Microsoft.Data.Sqlite.Core `10.0.9`, SQLitePCLRaw bundle `3.0.3`; one backend process at a time, independent participant process/database. Synthetic data only.
- Actual native `WorkflowRunner`, default activity input/invocation pipeline, scheduler, callback serialization, `FaultSignal`/`RecoverFromFault`, `DefaultCommitStateHandler` and EF workflow-instance store. The lightweight `Elsa` registration bundle plus Runtime/EF SQLite are referenced; the resolved dependency graph contains no BPMN or `Elsa.Server.Web` library.
- Experiment-only `ProbeScope`, explicit participant activity, root JSON-map codec, deterministic key allocator, inline Data Protection captures, retained-cause wrapper and commit observer. The controller does not execute workflow transitions, perform persistence in place of Elsa, repair native counts or implement a second scheduler.
- Ambient transaction is **off** except in the dedicated failure case. Snapshot/replay results cannot establish cross-store atomicity. No additional variable state is required for this fixture; handler failure also creates an ordinary native bookmark, whose atomic store participation remains unproved.
- Each scope is a single activation, with two serial native children and paired handlers. Its persisted seed is the experimental scope identity. Native work/callback nodes and active context IDs survive; a typed recovery-identity channel in native work metadata is **not** implemented. Correlation through this single serial scope is narrower than the proposed product contract.
- The prototype retains forward/undo context IDs and a current attempt identity per entry. It does not implement separate immutable forward/undo attempt histories or qualify stale local attempt-result/callback redelivery.
- The mandatory commit lives inside the explicit effect-aware activity after resolved input access and before HTTP. This proves that adapter's boundary, not a global invocation guarantee for arbitrary activities. Frozen protected request/result material is read for undo, including the participant-required captured reservation ID.
- Workflow/handler contract pins are retained in the map. The hardcoded supplied graph is not a persisted definition/handler resolver; its mismatch control deliberately demonstrates this limit. No publication, Studio or generic activity support is claimed.

## Measured matrix

The final matrix at `/tmp/elsa-e1-r5` ran all 16 cases, with fresh backend PIDs after every selected SIGKILL. Exact PIDs, snapshot revisions/hashes, native contexts/work/callbacks, synthetic operation/undo keys and independent participant receipts/counts are in [evidence-summary.json](evidence-summary.json). The generator checks every expected case and rejects unexpected harness/assertion failures. Exposed gaps are explicitly classified and are not passing E1 evidence.

| Case / actual barrier | Observation |
|---|---|
| Baseline | Reserve slots 0 then 1; definite-rejection child faults through native middleware; undo slots 1 then 0. Each logical reserve/undo count is one, both participant operations closed. Native Finished, no incidents, root fault count zero; Compensated journal survives completed-child cleanup. |
| Scope activation and committed-but-unacknowledged scope | Seed plus first native work/callback persist together. At kill, no participant call; restart uses the same seed and queued child. |
| Before intent acknowledgement | No participant call at the precommit cut. When the Armed entry is absent, restart derives the same operation identity from the persisted seed/slot. |
| Intent durable but acknowledgement withheld | `WorkflowStateCommitted` blocks after native persistence and before the activity's awaited `CommitAsync` returns. No participant call; restart reuses the committed entry and key. This is a controlled unknown-acknowledgement cut, not an arbitrary database/network-failure simulation. |
| After acknowledged intent, before forward HTTP | Armed request, operation keys and exact active native context survive new-process decode; the participant sees one effect. |
| Forward committed, result not locally recorded | Slot 1 replays with its original operation key; participant records a deduplicated call and one effect. The original protected request survives unchanged. The repeated receipt supplies the captured reservation ID before undo; no result is invented. |
| Handler reserved / before undo | Native queued/active handler work, attempt identity, stable undo key and protected target survive. Exactly one undo effect. |
| Undo committed, receipt not locally recorded | Replay uses the same undo key; participant records one logical undo effect. |
| Between handlers | Native queued next handler/callback restores; neither obligation is skipped. |
| Settled / before default handoff | Compensated journal and selected handoff work persist together; fresh process completes the real native parent callback path, root Finished/count zero, no incident, one default continuation recorded. |
| Settled / before selected fault handoff | Stored Fault choice survives a changed restart environment requesting Compensated. Native fault middleware produces one retained-cause-wrapper incident, root/scope/handoff fault counts one, final Faulted; Compensated journal remains and the default continuation count is zero. This is not a real publication/version-repository test. |
| Fault owned / before native recovery bookkeeping | **Gap:** persisted root and scope counts one plus incident one; original cause and ownership reservation retained, undo queued. Restart performs both participant undos once and reaches Compensated, but native final Finished still has incident one/root count one. Middleware's post-handler `RecoverFromFault` was lost. No count patch is applied. |
| Participant deduplication disabled | **Risk exposed:** lost slot-1 forward response produces reserve effect count two for slot 1; slot 0 remains one, both undo counts remain one and both close. The assertion specifically requires this direction/slot duplication. |
| Handler rejects undo | Running/Suspended, AwaitingIntervention, native bookmark retained; protected original forward cause and separate recovery cause present; no outward handoff or synthetic successful undo. |
| Optional EF ambient transaction enabled | **Gap:** native commit throws `AmbientTransactionWarning` / `InvalidOperationException`: SQLite provider does not support ambient transactions. Participant call count zero. Suppressing the warning is not tested or recommended as atomicity proof. |

## Native store/restore controls

The final controls at `/tmp/elsa-e1-controls-r2` use unchanged native EF/state-extraction services. Each control has its own backend/participant fixture. Controller copies of original/injected snapshot bytes are external evidence only; they do not provide native fail-closed preservation.

| Control | Observed native gap |
|---|---|
| Stale owner/revision | Two actual EF loads; newer revision 12 / owner B saved, then stale revision 3 / owner A saved successfully and became canonical. Root journal revision/owner fields are not commit-time CAS. No enabled production fence exists in this experiment, so its rejection half remains unproved. |
| Corrupt outer JSON | EF loader returns an existing entity with null WorkflowState. Ordinary native `SaveAsync(existing)` persists literal `null`, replacing original opaque bytes. No engine invocation is needed to expose this loss. |
| Outer compression mismatch | Valid JSON bytes marked GZip follow the same null-state fallback and overwrite on ordinary resave. Distinct from corrupt JSON and inner schema. |
| Unknown inner schema 999 | Experiment codec prevents participant invocation and preserves the unknown map value, but admission occurs after native state application; native workflow terminalizes Faulted and rewrites the outer snapshot. Original opaque bytes are not retained in the row. This is insufficient fail-closed admission/nonterminal inspection. |
| Missing queued work node | Native state extraction omits required work, changes the snapshot and fails to continue the obligation correctly. No admission guard preserves the invalid original work snapshot. |
| Missing callback owner / child | Native extractor omits the required callback. Queued slot-1 reservation still invokes the participant; both reservations remain open afterward. Required-reference validation must happen before any effect or omission. |
| Mismatched supplied definition graph | Resuming state pinned to v1 with a supplied v2 graph is accepted by direct native runner; final root definition is v2 while journal pin remains v1. The experiment's pins do not establish a strict resolver. LocalWorkflowClient's normal resolver and external handler-version resolution are not qualified by this direct-runner control. |

## Required correction before another feasibility decision

1. Select and prove a native provider transaction/commit protocol. SQLite needs explicit shared connection/transaction participation across required EF stores or a different accepted provider/profile; its current ambient wrapper is unsupported. Do not silently change global transaction defaults or ignore the warning.
2. Add a native persistence-owned expected revision and owner-epoch/CAS fence, checked at the actual write. Prove enabled stale rejection and a deliberately disabled negative control together. Definition `Version` is not a write revision.
3. Add compensation-aware outer hydration and restore admission before null fallback, reference omission or instance mutation. Trusted persisted obligation identification must survive an unreadable payload. Preserve original opaque bytes, reject empty replacement, validate schema/pins/work/callback owner and child references, and expose a nonterminal blocked result.
4. Establish an atomic native claimed-fault handoff/acknowledgement or a proven repairing restore protocol. Captured original cause, scheduled undo and native incident/count cleanup must not split across the demonstrated cut. Existing asymmetric `RecoverFromFault` cannot safely be replayed or hand-adjusted without durable ownership/acknowledgement evidence and retained context references.
5. Qualify exact definition/handler resolution and typed recovery identity in work/context metadata. The explicit adapter and hardcoded graph are not sufficient production contracts.

These findings require ADR/runtime/persistence review, not a silent claim that the prototype is production-ready. The first coherent corrective leaf should combine fail-closed raw hydration and write fencing with the owning native persistence boundary, or select a different bounded provider first; fault-handoff integration remains an independent mandatory gate. No seam/API/schema is frozen here.

The selected fault path deliberately uses `RetainedForwardFailureException` with a protected original-cause reference. It **changes the observable CLR fault type**; arbitrary exception reconstruction and type-specific catch compatibility are unproved. E3/owner acceptance is required before describing this as the supported original-fault contract.

Still unproved: actual stale-owner rejection, cross-store atomicity, mandatory generic invocation admission, strict persisted workflow/handler version resolution, missing-capture admission, duplicate/stale local callbacks/attempt results, asynchronous late-forward/closed-key refusal, fail-closed nonterminal restore, root finalization/explicit confirmation, destructive mutation guards, full tenant/auth/projection/source-disclosure behavior, production capture/key-retention policy, O2 cancellation, real Studio journeys/accessibility, and wider/provider/composition support. E2/E3 and M1–M5 are not advanced by these subset results.

## Commands, verification and retained attempts

```sh
dotnet build core/experiments/NativeCompensation.E1/NativeCompensation.E1.csproj -f net10.0 --nologo -p:TargetFrameworks=net10.0
python3 core/experiments/NativeCompensation.E1/run.py /tmp/elsa-e1-r5
python3 core/experiments/NativeCompensation.E1/controls.py /tmp/elsa-e1-controls-r2
python3 core/experiments/NativeCompensation.E1/summarize.py /tmp/elsa-e1-r5 /tmp/elsa-e1-controls-r2 core/experiments/NativeCompensation.E1/evidence-summary.json --controls-source-head 17a27e78d6c6afaef1b9c1a302b52442d30894f1
git diff --check
```

Use fresh output directories on reproduction. No database, key-ring, binary build output or protected capture content is committed. The summary exports synthetic identities/receipts, native state counts/work/callback references and capture/content hashes only. Raw local fixtures retain both databases, persisted protection keys, process events and original/injected bytes for review.

Build attempts are retained in `/tmp/elsa-e1-build*.log`: build 1 failed on harness-only `InputAttribute.IsSensitive`; build 2 failed on missing AddElsa/registry convenience references; build 3 failed on missing EF extension/registry namespaces; build 4 succeeded with zero warnings/errors; build 5 succeeded after prototype activation correction; build 6 succeeded after making the resave diagnostic null-safe, with 22 existing transitive documentation/package-manifest warnings and no errors. Global build-slot waits were respected; unrelated processes were not terminated or bypassed.

Native attempts were retained rather than recast as success: r1 participant rejected HttpClient chunked JSON because the harness assumed Content-Length (zero effects); r2 controller selector/TextIO buffering stalled and its own backend was stopped (no proof counted); r3 baseline passed after a queue-based reader; r4 exposed the prototype's seed-before-first-work gap; r5 corrected that prototype ordering and ran the final matrix. Control r1 proved outer overwrite but its diagnostic then dereferenced the null state; control r2 makes that output null-safe and asserts the exact observed gaps. Python syntax checks and whitespace checks accompany the process fixtures; no broad solution test or browser proof is claimed.

## Review follow-up: assertions rechecked against retained evidence

After frozen candidate `d163c7b285a6f08001a6a9db1dc23a601ff11719`, this harness-only review batch centralizes live-controller and retained-evidence assertions in `validation.py`. It strengthens native status/substatus, exit codes, actual SIGKILL/fresh PID, pre-intent participant absence, persisted cut state, fault-owned undo/closure counts, intervention bookmarks and missing-work/callback-reference outcomes. The exporter now checks the exact expected case-name sets, duplicates and directory/case agreement. Controllers reject unknown case names and exit nonzero on recorded failures; .NET host selection supports `E1_DOTNET` or `PATH`.

The strengthened assertions were rechecked against the original 16 r5 and eight control-r2 raw records. All 24 hashes still match the committed evidence summary. No C# behavior changed, no .NET build or native execution was repeated, and the original execution source heads above remain authoritative. The regenerated summary is byte-identical to the frozen summary; these are new assertions over retained observations, not fresh runtime results. The historical r4 native patch's blank-line whitespace was corrected and its application check was repeated against the recorded f3d source.

Fifteen cheap Python tests cover retained-evidence validation/hashes, relocated raw-only copies with original-path reads denied, optimized-Python import/CLI rejection before controller execution, coherent mutations of native status/exit/kill/PID/cut-state, missing-work and callback outcomes, missing undo/bookmark evidence, case-set/name/duplicate errors, empty recorded failures, controller failure exit status, host override and protected-summary sanitization:

```sh
python3 -m unittest discover -s core/experiments/NativeCompensation.E1 -p test_validation.py -v
```

The tests default to the retained `/tmp` directories above; set `E1_MATRIX_EVIDENCE` and `E1_CONTROL_EVIDENCE` to relocated evidence directories when needed. At that point they launched no .NET or fixture-participant process; later synthetic child-process coverage is documented below. E1 remains Partial and all previously recorded gates remain open.

The final review correction after `0e6f468c40b0e58ae68c84cfa4c45b69f9fac365` resolves hashes under each configured evidence root and case name, rather than following historical absolute paths in the summary. Validation explicitly refuses Python `-O`/`PYTHONOPTIMIZE`, which would remove its assertion gates. The status mutation changes the observed result, result event and persisted state together and verifies rejection by the native expected-status gate. These checks use the unchanged retained records or isolated copies; no fresh native execution/build is implied.

## Import-rehearsal inventory integration correction

CI's Dependabot project-discovery check exposed the new experiment as an unclassified
out-of-solution manifest. Running `ruby scripts/integration-program/test_dependabot_discovery.rb`
at `d0ac16665706ac310f96b43a0b007fa586a6d6fe` reproduced the failure: 15 runs,
7612 assertions, one complete-partition failure naming this project's manifest.
The correction extends the existing documented probe disposition with an explicit
Core experiment list, included in the complete unselected union. The regression
checks each listed experiment is tracked, explicitly `IsPackable=false`, and outside
`Elsa.sln`; an unlisted future experiment still fails. Documentation is read once and
its parsed lists are reused. No updater/workflow configuration or production source
changed, and all batch counts, selectors and feed policy checks remain intact.

After correction, the same Ruby command passes: 16 runs, 7630 assertions, zero
failures/errors/skips. The 15 Python tests were also rechecked without native process
execution. This narrow inventory integration adds the discovery test and its owning
document to the experiment's changed-file scope; it does not change the recorded E1
observations, their hashes, or readiness.

## Post-measurement backend cleanup and fixture style review

Greptile reproduced malformed backend stdout raising before cleanup, leaving the
backend alive and losing its PID, earlier events and stderr. The controller now
terminates only its own still-live child in a `finally` path, waits/reaps it, joins
the stdout/stderr readers, and carries PID/events/stderr plus controller/reader
errors in a structured failed phase. Both matrix and control runners use the same
phase helper, so a failure is retained in `evidence.json` and causes a nonzero
controller exit. Reader completion/failure is signaled even when stdout iteration
throws. Cheap Python child-process tests cover a valid event followed by malformed
output and a hanging timeout; they assert child reaping and retained event/stderr
diagnostics and run no .NET, participant or database process.

The four experiment C# files now brace their conditional bodies as required by
repository style. `Program.cs` registers `HttpClient` through the host-owned
singleton factory and uses `using var` for the host while preserving `StopAsync` in
`finally`. These are style/resource-ownership corrections only; they are not
byte-identical to the frozen measured fixture source. The r5 source head remains
`f3d4b31863dc7e2a029d6e93454a25a905e9b813`, and the controls source head remains
`17a27e78d6c6afaef1b9c1a302b52442d30894f1`. No C# build or native execution was
repeated for these corrections; the original raw files, 24 evidence hashes and
summary remain unchanged. E1 remains Partial and M1 Not Ready.

Follow-up controller review now treats stdout EOF as a signal to wait for the child
to exit within the remaining deadline, rather than terminating a child that closes
stdout before normal exit. A deadline overrun still terminates and reaps only that
child. Joined stdout-reader failures are promoted to controller errors even when a
cut barrier ends event consumption first. Two synthetic Python-child regressions
cover delayed normal exit after stdout EOF and a synchronized invalid-UTF-8 reader
failure after a cut barrier. At that review point, the full cheap validation suite
passed 20 tests,
including the retained 24 evidence-hash check; no .NET build or native execution was
run, and no E1 observation or readiness claim changed.

Control validation also requires the recorded inspection child to exit successfully
for every control that performs inspection; a negative regression covers all six
such controls. The cheap suite now has 25 passing tests, with the same retained
evidence hashes and no change to recorded observations or readiness.

Both controllers now initialize evidence before participant startup and share one
owned-process helper for the bounded port handshake, stdout/stderr capture, teardown
and reaping. Premature exit, malformed port output and a stalled handshake become
recorded controller failures; `evidence.json` is still written with the PID, exit,
stdout, stderr and cleanup status. Synthetic Python-child regressions exercise those
failures and both controller evidence paths, then verify the child is reaped. These
also verify a valid port enters the controller body before context teardown reaps the
still-live child. These are harness lifecycle checks only; the fixture participant,
.NET backend and database were not run again. The retained 24 raw evidence hashes and
summary are unchanged.

A synthetic main-thread `KeyboardInterrupt` after a child-ready signal is recorded
and rethrown unchanged by both controllers; each still writes evidence and reaps its
owned child. This exercises interruption during the handshake wait without running
the fixture participant or changing the retained observations.
