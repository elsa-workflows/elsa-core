# Opt-in: interrupt workflows still running at host stop

**New option, off by default.** `GracefulShutdownOptions.InterruptOnHostStop` lets a host-stop drain interrupt the execution cycles that are still running at its deadline, instead of force-cancelling them. With the default `false`, nothing changes.

## Why

A force-cancelled cycle ends `Finished`/`Cancelled` when its runner commits after the cancel, which it does as soon as the running activity returns. The drain leaves such rows alone (#8419), so the instance is never recovered. On a host stop the process ends anyway, so the cycle does not need to be cancelled to stop it.

## With the option on

On a host stop whose drain deadline passes:

- The drain does not cancel the cycle. The workflow keeps its status, and nothing reports it cancelled or finished.
- Later commits of the interrupted cycle are dropped, so the runner, which keeps going until the process ends, cannot overwrite what the drain persisted.
- A committed checkpoint (scheduled work or an executing activity) is marked `Running`/`Interrupted`. The interrupted-workflow scan on the next start continues it from there.
- A resume that never committed leaves the row at the suspension it resumed. The drain does not mark that row `Interrupted`, because the scan would run the workflow again from its root. It queues the resume again on the bookmark queue, with the same bookmark and input.
- A fresh run that no trigger started and that never committed is persisted as not started, marked `Running`/`Interrupted`, and started by the scan.
- A fresh run started by a trigger and never committed is left to its trigger. Only the `WorkflowInterrupted` log entry is written, as before.
- The cycles are reported in `DrainOutcome.ExecutionCyclesForceCancelledCount` and `ForceCancelledInstanceIds`, and get a `WorkflowInterrupted` log entry.

Shell deactivation and operator force keep cancelling.

## What to do

- Set `InterruptOnHostStop = true` in `ConfigureGracefulShutdown`, or, on a host that uses shell features, in `WorkflowRuntimeFeature.GracefulShutdown`, to opt in.
- Commits are dropped by `ExecutionCycleAwareCommitStateHandler`, the default `ICommitStateHandler` of the workflow runtime. A custom commit state handler that does not wrap it keeps committing after the interrupt.
- A custom `IExecutionCycleRegistry` that lists a handle as soon as it registers it should implement the `BeginCycle` overload that takes a `configure` callback and run the callback before it lists the handle. A drain that sees the cycle before the callback ran writes only the `WorkflowInterrupted` log entry for an uncommitted resume or fresh run; it neither queues the resume again nor persists the run as not started.
- Activities that ran after the last commit run again on recovery. This is the same at-least-once rule as for any other interrupted instance.
- A resume that came from the bookmark queue is queued twice: once by the drain, and once by its own queue item, which is deleted only after the resume returns. When the bookmark is burned on resume (the default), the second item finds no bookmark and is purged later. A bookmark that is not burned is resumed twice.
- Only commits are dropped. If the cut-off runner finishes before the process ends, the notifications it publishes outside a commit still go out; for example, `WorkflowExecuted` of a finished child resumes a parent that waits in `DispatchWorkflow`. That resume uses the child's output in memory, but the child's own commit is dropped. The parent can then commit a result that the child's stored state does not record, and the recovered child runs again from its last checkpoint and can produce a different result. Consider this for workflows that use `DispatchWorkflow` with `WaitForCompletion`.
- A commit that had already started when the drain interrupted the cycle is not dropped and can land after the drain's write. It stores the runner's newer checkpoint, which the recurring `RestartInterruptedWorkflowsTask` restarts in its first run after the checkpoint is older than the inactivity threshold (with the defaults, within about 10 minutes), or the finished workflow.
