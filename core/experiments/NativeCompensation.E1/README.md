# Native compensation E1 experiment

Temporary evidence harness for [#8930](https://github.com/elsa-workflows/elsa-core/issues/8930), against the native compensation design packet at `51d316882e6a79376145aef1283aa4004a3f400d`.
Delete this project after E1's findings have been incorporated into an accepted native runtime/persistence design and lasting regression tests. It deliberately stays outside the solution, package/release targets and application startup: a separate console backend is needed for abrupt process-kill evidence. This is experimental evidence, not a compensation product or supported profile.

The backend uses the real `WorkflowRunner`, native activity scheduling and completion callbacks, `FaultSignal` middleware, `DefaultCommitStateHandler`, EF instance store and SQLite provider. The Python controller never implements workflow transitions. A separate Python process owns a separate participant database; HTTP Reserve/Undo/Lookup use stable operation/undo keys and a closed-key rule. Barriers require an explicit controller acknowledgement; timeouts only detect stalled processes. Process interruption uses SIGKILL, without graceful drain.

Build once, then execute the already-built DLL (one local .NET build/testhost at a time):

```sh
dotnet build core/experiments/NativeCompensation.E1/NativeCompensation.E1.csproj -f net10.0 -p:TargetFrameworks=net10.0
python3 core/experiments/NativeCompensation.E1/run.py /tmp/elsa-native-compensation-e1-evidence --case baseline
```

Use a fresh output directory for each run; evidence is never overwritten. Omit `--case` to run the matrix. Every case writes exact backend/participant PIDs, barrier events, native commit contexts/work/callbacks, EF snapshot hashes/content, journal revisions and independent participant receipts/counts to `evidence.json`, alongside both SQLite databases and local Data Protection keys. All data is synthetic. Keep generated database/key directories outside Git.

Run `controls.py <fresh-output-directory>` for the eight native store/restore negative controls. `summarize.py` exports the final matrix/control observations without protected payloads or keys; see the report for the exact command. Known native gaps are successful experimental observations, not passing product acceptance. The exporter rejects unexpected harness/assertion failures.

The project adds an experiment-only scope, explicit effect-aware activity, map codec and injection observer. It does not add a global invocation hook, scheduler, persistence transaction or write fence. Scope seed allocation, handler reservation, safe persisted-cause wrapper and pinned handoff are prototypes. The hardcoded graph is not a durable definition repository or proof of exact handler-version resolution. Captures use persisted ASP.NET Data Protection keys and tenant/instance/purpose separation; broad API/log disclosure and authorization remain E2 work.

The `--ambient` case measures Elsa's existing optional `EFCoreWorkflowCommitTransaction`. Cases with it disabled are clearly weaker exploratory snapshot tests and cannot establish cross-store atomicity. The scope activation and fault-ownership cuts intentionally expose gaps rather than repairing native bookkeeping in the harness. The selected fault handoff uses an explicit retained-cause wrapper, never arbitrary CLR exception reconstruction or a type-specific catch guarantee.

See `REPORT.md` for the measured results and unresolved gates.
