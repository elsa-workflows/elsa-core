# Workflow Execution Throughput Analysis: Log Record Persistence

## Executive Summary

Current Elsa workflow execution is partly gated by persistence work that happens during commit. In the default runtime path, `WorkflowRunner.RunAsync` executes the workflow pipeline first and then awaits `ICommitStateHandler.CommitAsync` before returning. The default implementation, `DefaultCommitStateHandler`, persists bookmarks, activity execution records, workflow execution log records, variables, and finally the workflow instance, all via awaited operations. This means the caller does not observe workflow execution as complete until commit work has finished.

This validates the core observation that log record persistence can reduce workflow throughput, especially when a workflow produces many activity execution records and workflow execution log records. However, there is an important nuance: workflow execution does not always wait only on the final post-pipeline commit. Elsa can also perform commits inside the workflow and activity middleware pipeline when commit strategies are configured, which means some log persistence overhead can occur during pipeline execution as well.

For OpenTelemetry, the safest conclusion is that workflow spans created strictly around the workflow execution pipeline should exclude the final post-pipeline commit performed by `WorkflowRunner`, but they may still include log-related overhead that occurs inside the pipeline itself, such as activity execution record capture and any in-pipeline commits caused by configured commit strategies. In other words, OTEL workflow spans likely match the execution pipeline boundary, but not necessarily a pure “business logic only” duration.

The most promising opportunities to improve throughput are: reducing the amount of log data produced, reducing commit frequency, moving non-critical notifications off the synchronous hot path, and optionally introducing asynchronous queued log sinks for users who accept best-effort log durability.

---

## Scope

This note reviews the current runtime behavior around workflow execution throughput, with specific focus on:

- when workflow execution is considered complete,
- how and when log records are persisted,
- whether log persistence blocks the caller,
- what this means for OTEL workflow spans, and
- what improvements are most likely to help throughput.

The analysis is based on the current implementation in:

- `src/modules/Elsa.Workflows.Core/Services/WorkflowRunner.cs`
- `src/modules/Elsa.Workflows.Runtime/Services/DefaultCommitStateHandler.cs`
- `src/modules/Elsa.Workflows.Runtime/Services/StoreActivityExecutionLogSink.cs`
- `src/modules/Elsa.Workflows.Runtime/Services/StoreWorkflowExecutionLogSink.cs`

Additional supporting code was also reviewed to clarify commit strategy behavior, activity record capture, and notification publishing.

---

## Confirmed Observations

### 1. `WorkflowRunner.RunAsync` waits for commit before returning

In `WorkflowRunner.RunAsync(WorkflowExecutionContext)`, Elsa:

1. sends `WorkflowExecuting`,
2. sends `WorkflowStarted` when appropriate,
3. executes the workflow pipeline via `pipeline.ExecuteAsync(...)`,
4. extracts workflow state,
5. sends `WorkflowFinished` if the workflow finished,
6. sends `WorkflowExecuted`,
7. awaits `commitStateHandler.CommitAsync(...)`,
8. and only then returns `RunWorkflowResult`.

This confirms that the caller of `IWorkflowRunner.RunAsync` does not see the workflow execution as complete until commit has completed.

### 2. `DefaultCommitStateHandler` persists state in the expected order

`DefaultCommitStateHandler.CommitAsync` performs the following operations in order:

1. persist bookmarks,
2. persist activity execution log records,
3. persist workflow execution log records,
4. save variables,
5. save the workflow instance/state,
6. clear in-memory execution log entries,
7. clear completed activity execution contexts,
8. execute deferred tasks,
9. publish `WorkflowStateCommitted`.

This matches the observed ordering.

### 3. The default log sinks block commit synchronously

The current default sinks are synchronous from the caller’s perspective:

- `StoreActivityExecutionLogSink.PersistExecutionLogsAsync` gathers dirty activity execution contexts, maps them to `ActivityExecutionRecord` instances, writes them via `activityExecutionStore.SaveManyAsync`, clears taint, and publishes `ActivityExecutionLogUpdated`.
- `StoreWorkflowExecutionLogSink.PersistExecutionLogsAsync` extracts `WorkflowExecutionLogRecord` instances, writes them via `store.AddManyAsync`, and publishes `WorkflowExecutionLogUpdated`.

Because these operations are awaited by `DefaultCommitStateHandler`, log persistence is currently on the synchronous hot path.

### 4. Large log volumes can directly reduce throughput

This follows naturally from the above. If a workflow produces many activity execution records and/or workflow execution log records, commit duration increases, and therefore throughput decreases.

This is especially true for activity execution records, which tend to carry more serialized state than workflow execution log records.

---

## Corrections and Important Nuances

### 1. A workflow can be in a finished state before the final commit completes

A useful distinction is:

- **workflow reaches a finished in-memory state**, versus
- **workflow runner call has completed and state has been durably committed**.

Inside the workflow pipeline, the workflow can transition to `Finished` before the final commit in `WorkflowRunner`. In addition, `WorkflowFinished` is published before the final call to `commitStateHandler.CommitAsync`.

So the strict statement is not:

> the workflow finishes only after logs are flushed

but rather:

> the workflow runner does not complete until after commit has completed, and commit includes log persistence.

That is a more precise description of current behavior.

### 2. Log persistence is not only post-pipeline

This is the main nuance that affects the OTEL conclusion.

Although `WorkflowRunner` performs a final commit after `pipeline.ExecuteAsync(...)`, Elsa can also commit **inside** the workflow and activity middleware pipeline when commit strategies are configured.

Examples:

- workflow-level commits can happen from `DefaultActivitySchedulerMiddleware`, and
- activity-level commits can happen from `DefaultActivityInvokerMiddleware` before and/or after activity execution.

Therefore, log persistence can occur:

- after the pipeline, via the final commit in `WorkflowRunner`, and
- during the pipeline, when commit strategies request a commit.

### 3. Activity execution record capture already adds in-pipeline overhead

Even before database writes occur, activity execution records may already be captured during activity completion. That capture work includes mapping, serialization, payload shaping, and potentially compression of activity state.

This means there are two different kinds of overhead to keep in mind:

- **log preparation cost**, and
- **log flush/store cost**.

Moving only the database write to a background channel helps with the second category, but not necessarily all of the first.

---

## OTEL Span Implications

## Short Answer

The original conclusion is **mostly directionally correct, but too absolute**.

A workflow execution span that measures only the workflow middleware pipeline should generally exclude the **final post-pipeline commit** performed by `WorkflowRunner`. However, it may still include log-related overhead that occurs within the pipeline itself.

## Detailed Interpretation

If Elsa.OpenTelemetry creates workflow spans around `pipeline.ExecuteAsync(...)`, then those spans likely:

- **exclude** the final `commitStateHandler.CommitAsync(...)` call in `WorkflowRunner`,
- **include** activity execution work,
- **include** activity execution record capture performed during execution,
- and **may include** log persistence caused by in-pipeline commit strategies.

Therefore, the strongest safe statement is:

> OTEL workflow spans likely align with the workflow execution pipeline boundary, not necessarily with the full end-to-end duration observed by the caller of `WorkflowRunner.RunAsync`.

A related practical takeaway is that two different timings may both be useful:

- **pipeline execution duration**,
- **end-to-end execution plus commit duration**.

Those are not always the same number.

---

## Why Activity Execution Records Are Likely the Bigger Cost

Not all log records cost the same.

### Workflow execution log records

These are comparatively lightweight. They are extracted from `WorkflowExecutionContext.ExecutionLog` and mapped into `WorkflowExecutionLogRecord` instances.

### Activity execution records

These are typically more expensive because they may include:

- activity input state,
- output values,
- properties,
- metadata,
- journal payload,
- exception payload,
- serialization,
- and possible compression.

As a result, activity execution records are likely the first place to focus when throughput is being hurt by log persistence.

---

## Additional Throughput Factors Beyond the Raw Store Write

The total synchronous cost of commit is not only the database insert or update.

### 1. Notification publishing after persistence

Both default log sinks publish notifications after writing records:

- `ActivityExecutionLogUpdated`
- `WorkflowExecutionLogUpdated`

In the default mediator configuration, notifications are published sequentially unless configured otherwise. This means notification handlers can add additional latency directly to the same synchronous path.

### 2. Real-time progress updates can add more work

When runtime/API features are enabled, notification handlers may:

- query activity execution statistics,
- broadcast updates over SignalR,
- publish workflow instance updates.

That means observed “log persistence time” may actually include:

- record mapping,
- store writes,
- post-write notifications,
- follow-up queries,
- and real-time broadcast work.

### 3. Variable and workflow-instance persistence also remain on the hot path

Even if log persistence were optimized, commit still includes:

- variable persistence, and
- workflow instance save.

So log persistence is an important throughput factor, but not the only one.

---

## Assessment of the Background Channel Proposal

## Summary Assessment

The idea is sound as an **optional throughput optimization**.

If users accept the possibility of losing some recently produced log records during abrupt process termination, then moving log persistence to an in-memory background channel can reduce end-to-end workflow latency and improve throughput.

## Why it helps

A background channel removes the store write from the synchronous commit path. Instead of awaiting record persistence inline, Elsa could:

1. capture or construct immutable log record payloads,
2. enqueue them to a channel,
3. return from commit sooner,
4. let a background worker flush records in batches.

This can reduce:

- workflow completion latency as observed by the caller,
- contention on the hot path,
- and potentially overall database pressure if batching is used.

## Risks and trade-offs

The main risk is loss of enqueued-but-not-yet-flushed records on crash or abrupt shutdown.

Other trade-offs include:

- eventual consistency between workflow state and log availability,
- need for queue sizing and backpressure,
- memory growth if producers outpace consumers,
- possible reordering concerns if multiple queues/workers are used,
- more operational complexity.

## Durable outbox comparison

Your reasoning about a durable outbox is correct.

If the system must durably preserve logs before continuing, then some durable write still needs to happen on the hot path. Whether that write is:

- the final log records themselves, or
- outbox records that will later be forwarded,

there is still synchronous persistence cost.

So a pure in-memory background channel is the approach that yields the clearest throughput improvement, but only by accepting weaker durability guarantees.

---

## Recommended Design Direction

A good design would make asynchronous log persistence **opt-in**, not mandatory.

### Suggested model

Introduce optional queued implementations of:

- `ILogRecordSink<ActivityExecutionRecord>`
- `ILogRecordSink<WorkflowExecutionLogRecord>`

These could:

1. capture immutable records on the foreground thread,
2. enqueue them to a bounded channel,
3. let a background worker batch and flush them,
4. publish completion/failure metrics.

### Important design decisions

The following decisions should be explicit:

- **bounded vs unbounded queue**
- **overflow behavior**
  - block producer
  - drop newest
  - drop oldest
  - fallback to synchronous write
- **flush policy**
  - immediate
  - batch size based
  - time window based
- **shutdown behavior**
  - drain queue on graceful stop
  - cancel immediately
- **ordering guarantees**
  - per workflow instance
  - best effort only

### Preferred default posture

For production safety, a reasonable default would be:

- keep current synchronous behavior as default,
- provide a clearly documented best-effort async mode,
- expose metrics and health signals for queued mode.

---

## Lower-Risk Improvements to Try First

Before introducing eventual consistency for logs, there are lower-risk optimizations worth trying.

### 1. Reduce log volume and payload size

Especially for activity execution records:

- exclude unnecessary inputs/outputs from persisted records,
- reduce payload size where detailed audit data is not needed,
- review default log persistence settings for high-volume workflows.

This directly reduces both CPU and I/O cost.

### 2. Review commit strategy usage

If workflows or activities are committing too frequently, log persistence cost is paid repeatedly during execution.

Questions worth answering:

- Are default workflow commit strategies configured globally?
- Are activity-level commit strategies causing commits after many or all activities?
- Can some workflows tolerate fewer intermediate commits?

Reducing commit frequency may improve throughput significantly without changing durability semantics.

### 3. Move non-critical notifications off the synchronous path

Notification handlers related to progress updates or UI refresh are strong candidates for asynchronous dispatch, because they are often operationally useful but not required to determine workflow correctness.

### 4. Measure each commit segment independently

Before implementing changes, instrument and measure:

- bookmark persistence time,
- activity log mapping time,
- activity log store write time,
- workflow log extraction time,
- workflow log store write time,
- variable persistence time,
- workflow instance save time,
- notification publishing time.

This will show where the actual time is being spent in a given deployment.

---

## Recommended Next Steps

## Near-term

1. **Add metrics/timing around each commit step** in `DefaultCommitStateHandler` and both default log sinks.
2. **Measure typical and worst-case workflows** with emphasis on record counts and payload sizes.
3. **Review commit strategy configuration** to identify unnecessary in-pipeline commits.
4. **Review activity log payload content** and reduce what is persisted where possible.
5. **Assess notification-handler cost** after log persistence, especially real-time handlers.

## Medium-term

6. **Prototype a queued async log sink** for `ActivityExecutionRecord` first.
7. **Batch writes** in the queued sink to reduce database round-trips.
8. **Add queue metrics** such as depth, flush duration, dropped record count, and last successful flush timestamp.
9. **Document consistency guarantees** clearly for operators and developers.

## Longer-term / optional

10. If stronger durability is required, evaluate a **durable queue or outbox** and compare its actual cost against direct log writes.
11. Consider exposing separate measurements for:
    - workflow pipeline duration,
    - final commit duration,
    - total runner duration.

This would make both performance analysis and OTEL interpretation clearer.

---

## Final Conclusions

- The main observation is **confirmed**: by default, workflow completion as observed by the caller of `WorkflowRunner.RunAsync` is blocked by commit, and commit includes synchronous log persistence.
- The sequencing observed in `DefaultCommitStateHandler` is **correct**.
- The strongest correction is that **log persistence is not only post-pipeline**; it can also happen during the workflow/activity pipeline when commit strategies are configured.
- Therefore, the OTEL conclusion should be softened: workflow-execution spans around the pipeline likely exclude the final post-pipeline commit, but they may still include in-pipeline commit and log-capture overhead.
- The best immediate performance opportunities are likely:
  - reduce log payload volume,
  - reduce commit frequency,
  - offload non-critical notifications,
  - and introduce optional async queued log sinks for deployments that accept best-effort durability.

---

## Suggested One-Paragraph Shareable Summary

Elsa currently performs workflow state commit synchronously after workflow execution, and the default commit path includes persisting bookmarks, activity execution records, workflow execution log records, variables, and the workflow instance itself. As a result, `WorkflowRunner.RunAsync` does not return until log persistence and related commit work have completed. That said, workflows may already be in a finished state before the final commit, and commit work can also occur inside the execution pipeline when commit strategies are configured. This means log persistence can affect throughput both at the end of execution and, in some cases, during execution itself. The best next steps are to measure the individual commit steps, reduce unnecessary log payload and commit frequency, and consider an optional asynchronous queued log sink for environments where improved throughput is more important than guaranteed preservation of the last in-memory log records during a crash.

